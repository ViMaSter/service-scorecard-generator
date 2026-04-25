package checkruntime

import (
	"encoding/xml"
	"errors"
	"io"
	"os"
	"strings"
)

type ProjectXML struct {
	rootAttributes map[string]string
	elements       map[string][]string
	decodeErr      error
}

func LoadProjectXML(path string) ProjectXML {
	file, err := os.Open(path)
	if err != nil {
		return ProjectXML{decodeErr: err}
	}
	defer file.Close()

	decoder := xml.NewDecoder(file)
	result := ProjectXML{rootAttributes: map[string]string{}, elements: map[string][]string{}}
	var stack []string
	var current strings.Builder
	var sawRoot bool

	for {
		token, tokenErr := decoder.Token()
		if tokenErr != nil {
			if errors.Is(tokenErr, io.EOF) {
				break
			}
			result.decodeErr = tokenErr
			return result
		}

		switch typed := token.(type) {
		case xml.StartElement:
			if !sawRoot {
				sawRoot = true
				for _, attribute := range typed.Attr {
					result.rootAttributes[attribute.Name.Local] = attribute.Value
				}
			}
			stack = append(stack, typed.Name.Local)
			current.Reset()
		case xml.CharData:
			current.Write([]byte(typed))
		case xml.EndElement:
			if len(stack) == 0 {
				continue
			}
			value := strings.TrimSpace(current.String())
			if value != "" {
				result.elements[stack[len(stack)-1]] = append(result.elements[stack[len(stack)-1]], value)
			}
			stack = stack[:len(stack)-1]
			current.Reset()
		}
	}

	return result
}

func (p ProjectXML) DecodeErr() error {
	return p.decodeErr
}

func (p ProjectXML) RootAttribute(name string) string {
	return p.rootAttributes[name]
}

func (p ProjectXML) FirstElement(name string) string {
	values := p.elements[name]
	if len(values) == 0 {
		return ""
	}
	return values[0]
}

func (p ProjectXML) AllElements(name string) []string {
	return p.elements[name]
}
