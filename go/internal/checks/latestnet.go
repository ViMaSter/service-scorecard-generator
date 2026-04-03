package checks

import (
	"encoding/json"
	"fmt"
	"net/http"
	"regexp"
	"sort"
	"strconv"
	"strings"
	"sync"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type LatestNET struct {
	baseCheck
	NewestMajor int
	newestText  string
}

type latestNETReleaseData struct {
	ReleasesIndex []latestNETRelease `json:"releases-index"`
}

type latestNETRelease struct {
	ChannelVersion string `json:"channel-version"`
	LatestRelease  string `json:"latest-release"`
}

var latestNETNumberPattern = regexp.MustCompile(`\d+`)

var latestNETCache struct {
	sync.Mutex
	loaded      bool
	newestMajor int
	newestText  string
	loadErr     error
}

func NewLatestNET(client *http.Client) *LatestNET {
	newestMajor, newestText, err := loadLatestNETMetadata(client)
	if err != nil {
		panic(err)
	}
	return &LatestNET{baseCheck: newBaseCheck("LatestNET"), NewestMajor: newestMajor, newestText: newestText}
}

func loadLatestNETMetadata(client *http.Client) (int, string, error) {
	latestNETCache.Lock()
	defer latestNETCache.Unlock()
	if latestNETCache.loaded {
		return latestNETCache.newestMajor, latestNETCache.newestText, latestNETCache.loadErr
	}
	if client == nil {
		client = http.DefaultClient
	}
	response, err := client.Get("https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json")
	if err != nil {
		latestNETCache.loadErr = err
		latestNETCache.loaded = true
		return 0, "", err
	}
	defer response.Body.Close()
	var parsed latestNETReleaseData
	if err := json.NewDecoder(response.Body).Decode(&parsed); err != nil {
		latestNETCache.loadErr = err
		latestNETCache.loaded = true
		return 0, "", err
	}
	nonPreview := make([]latestNETRelease, 0, len(parsed.ReleasesIndex))
	for _, release := range parsed.ReleasesIndex {
		if !strings.Contains(release.LatestRelease, "preview") {
			nonPreview = append(nonPreview, release)
		}
	}
	sort.Slice(nonPreview, func(i, j int) bool {
		return compareChannelVersion(nonPreview[i].ChannelVersion, nonPreview[j].ChannelVersion) > 0
	})
	if len(nonPreview) == 0 {
		latestNETCache.loadErr = fmt.Errorf("no stable .NET releases found")
		latestNETCache.loaded = true
		return 0, "", latestNETCache.loadErr
	}
	newestText := nonPreview[0].ChannelVersion
	major, _ := strconv.Atoi(strings.Split(newestText, ".")[0])
	latestNETCache.newestMajor = major
	latestNETCache.newestText = newestText
	latestNETCache.loaded = true
	return latestNETCache.newestMajor, latestNETCache.newestText, nil
}

func compareChannelVersion(left string, right string) int {
	leftValue := numericVersionValue(left)
	rightValue := numericVersionValue(right)
	switch {
	case leftValue > rightValue:
		return 1
	case leftValue < rightValue:
		return -1
	default:
		return 0
	}
}

func numericVersionValue(channel string) int {
	parts := strings.Split(channel, ".")
	values := []int{0, 0, 0}
	for index := 0; index < len(parts) && index < len(values); index++ {
		values[index], _ = strconv.Atoi(parts[index])
	}
	return values[0]*10000 + values[1]*100 + values[2]
}

func (c *LatestNET) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	project := loadProjectXML(absolutePathToProjectFile)
	targetFramework := project.firstElement("TargetFramework")
	if targetFramework == "" {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "No <TargetFramework> element found in %v", absolutePathToProjectFile)}
	}
	if !strings.Contains(targetFramework, ".") {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "Service uses %v, latest available is %v; using .NET Framework deducts all points", targetFramework, c.newestText)}
	}
	number := latestNETNumberPattern.FindString(targetFramework)
	currentMajor, _ := strconv.Atoi(number)
	if currentMajor > c.NewestMajor {
		return []scorecard.Deduction{scorecard.NewDeduction(5, "Service uses (%v) latest available is only (%v)", targetFramework, c.newestText)}
	}
	if currentMajor == c.NewestMajor {
		return nil
	}
	offset := 100 - int(mathRound(((float64(currentMajor) / float64(c.NewestMajor)) * 100)))
	return []scorecard.Deduction{scorecard.NewDeduction(offset, "Service uses %v, latest available is %v (%v/%v=%v%%)", targetFramework, c.newestText, currentMajor, c.NewestMajor, 100-offset)}
}

func mathRound(value float64) float64 {
	if value < 0 {
		return float64(int(value - 0.5))
	}
	return float64(int(value + 0.5))
}

var _ Check = (*LatestNET)(nil)
