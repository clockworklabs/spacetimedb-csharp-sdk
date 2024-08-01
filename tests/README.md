# Running tests
You can use `dotnet test` (either in this directory or in the project root directory) to run the tests.

# Using a different SpacetimeDB version
To run tests using a local version of the `SpacetimeDB` repo, you can add a `nuget.config` file in the **root** of this repository.

The `scripts/write-nuget-config.sh` script can generate the `nuget.config`. It takes one parameter: the path to the root SpacetimeDB repository (relative or absolute).

Then, you need to `dotnet pack` the `BSATN.Runtime` package in the `SpacetimeDB` repo.

Example:
```bash
$ export SPACETIMEDB_REPO_PATH="../SpacetimeDB"
$ scripts/write-nuget-config.sh "${SPACETIMEDB_REPO_PATH}"
$ ( cd "${SPACETIMEDB_REPO_PATH}"/crates/bindings-csharp/BSATN.Runtime && dotnet pack )
$ dotnet test
```
