# BuiltForAKS

## About
This check returns **100 points** if the scanned service has a **pipeline** that deploys the project **to Azure**.  
This check also returns **100 points** if the scanned service's directory contains `.Test` or `.Common`.  

**NOTE: This check ignores files ending in `.yaml` by design. Files need to end in `.yml`.**

Applies a **100 point** deduction, if the `.yml` file starts with `onprem-`.  
Applies a **100 point** deduction, if no `.yml` file exists.  
Applies a **5 point** deduction for each `.yml` file present if **more than one** exist.  
Applies a **5 point** deduction, if a `.yml` file exists, but the filename doesn't start with `onprem-`, `azure-` or `build-`.  

## How to achieve 100 points
Services need to have precisely one accompanying `azure-pipelines.yml` file.  
Packages need to have precisely one accompanying `build-pipelines.yml` file.

## Why this check exists
Pipelines ensure that projects stay in a deployable state. They also reduce [toil](https://sre.google/sre-book/eliminating-toil/). 
