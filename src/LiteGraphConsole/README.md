# LiteGraphConsole

`LiteGraphConsole` is an interactive terminal shell for LiteGraph databases and endpoints. It installs as the `lg` command and is intended to feel similar to using `sqlite3`: choose a database or endpoint, enter statements, and end queries with `;`.

## Install

From the repository root:

```powershell
.\install-tool.bat
```

To reinstall after rebuilding:

```powershell
.\reinstall-tool.bat
```

To remove the tool:

```powershell
.\remove-tool.bat
```

The scripts package `src\LiteGraphConsole\LiteGraphConsole.csproj` and install it as a local global tool command named `lg`.

## Local Database

```powershell
lg --database litegraph.db --tenant 00000000-0000-0000-0000-000000000000 --graph 00000000-0000-0000-0000-000000000000
```

The database mode opens a SQLite-backed LiteGraph repository directly.

## Remote Endpoint

```powershell
lg --endpoint http://localhost:8701 --tenant 00000000-0000-0000-0000-000000000000 --graph 00000000-0000-0000-0000-000000000000 --token litegraphadmin
```

Endpoint mode posts queries to:

```text
/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/query
```

Use `--token` for bearer token authentication or `--security-token` for `x-token` authentication.

## One Query

```powershell
lg --database litegraph.db --tenant <tenant-guid> --graph <graph-guid> --execute "MATCH (n) RETURN n LIMIT 5"
```

## Script File

```powershell
lg --database litegraph.db --tenant <tenant-guid> --graph <graph-guid> --script queries.lg
```

Statements in script files are separated by semicolons. Semicolons inside quoted strings are preserved.

## Parameters

Parameters use JSON values:

```powershell
lg --database litegraph.db --tenant <tenant-guid> --graph <graph-guid> --param name='"Ada"' --execute "MATCH (n:Person) WHERE n.name = $name RETURN n"
```

Common parameter examples:

```powershell
--param age=36
--param active=true
--param profile='{"role":"engineer"}'
--param embedding='[0.1,0.2,0.3]'
```

## Interactive Commands

Inside the shell:

```text
.help
.show
.tenant <guid>
.graph <guid>
.database <path>
.endpoint <url>
.token <token>
.param set <name> <json>
.param unset <name>
.param clear
.read <path>
.mode pretty
.mode compact
.quit
```

Queries are buffered until a line ends with `;`.

```cypher
MATCH (n:Person)
WHERE n.name = $name
RETURN n
LIMIT 5;
```
