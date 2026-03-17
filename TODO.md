# TODO

## +item list syntax

blpy supports `+item` syntax for list-of-objects body properties (e.g. `+route`, `+license`, `+rule`, `+alert`). This is a significant feature that needs its own implementation phase.

Skipped integration tests waiting on this:
- `load-balancer rule create/delete` (+rule for forwarding-rules)
- `server action change-advanced-firewall-rules` (+rule for firewall-rules)
- `server action change-threshold-alerts` (+alert for threshold-alerts)
- `server action resize` (blpy always sends change_licenses wrapper)
- `server create` full test (+license)
- `vpc create` with +route

## blpy help compatibility gaps

| blpy | blnet | Issue |
|------|-------|-------|
| `--backups, --no-backups` toggle | `--backups BACKUPS` | Bool body props should be flags |
| `--vpc VPC` | `--vpc-id VPC_ID` | Option naming — blpy strips `_id` and resolves entity names |
| `--servers` | `--server-ids` | Option naming — blpy uses different name for array-of-id body props |
| `(default: "none")` on action commands | `(default: "table")` always | Per-command output defaults |
| `--async` / `--quiet` on action commands | not present | Action wait/progress not implemented |
| `SERVER` = "The ID or name of..." | `SERVER` = "The ID of..." | Entity lookup via `x-cli-entity-lookup`, `x-cli-entity-list`, `x-cli-entity-ref` |
| `Available fields:` section on list commands | not present | Field descriptions in help for list commands |
