set -ueo pipefail

SPACETIMEDB_REPO_PATH="$1"

cd "$(dirname "$(readlink -f "$0")")"

# Write out the nuget config file to `nuget.config`. This causes the spacetimedb-csharp-sdk repository
# to be aware of the local versions of the `bindings-csharp` packages in SpacetimeDB, and use them if
# available. Otherwise, `spacetimedb-csharp-sdk` will use the NuGet versions of the packages.
# This means that (if version numbers match) we will test the local versions of the C# packages, even
# if they're not pushed to NuGet.
# See https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file for more info on the config file,
# and https://tldp.org/LDP/abs/html/here-docs.html for more info on this bash feature.
cat >nuget.config <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <!-- Local NuGet repositories -->
    <add key="Local SpacetimeDB.BSATN.Runtime" value="${SPACETIMEDB_REPO_PATH}/crates/bindings-csharp/BSATN.Runtime/bin/Release" />
    <!-- Official NuGet.org server -->
    <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

echo "Wrote nuget.config contents:"
cat nuget.config
