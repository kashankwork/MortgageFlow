## Summary

- Describe the user-visible or architectural outcome.

## Verification

- [ ] `dotnet restore`
- [ ] `dotnet format --verify-no-changes`
- [ ] `dotnet build --configuration Release --no-restore`
- [ ] `dotnet test --configuration Release --no-build`

## Scope check

- [ ] This change stays within the current product scope.
- [ ] Deferred technologies remain deferred.
- [ ] Security/data-boundary impact is documented.
