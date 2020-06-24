# Custom Workflow Nodes

A collection of custom workflow nodes for GlobalCapture/GlobalAction, provided both for their useful functions to extended your workflows and as samples for developing your own custom nodes.

## Cron Import

The current import frequency is relative to when the engine service is started. This custom import node will allow workflow designers to more precisely define times, dates, and frequencies that the import action for a given workflow will run. The node is designed using the CRON pattern.

> The software utility cron is a time-based job scheduler in Unix-like computer operating systems (like Linux and MacOS).

### Cron Import - Configuration Options

- Frequency - a default setting for Import nodes that sets how often the node will be checked. Instead of determining how often the import will run, for this node, the setting is for how often the current time will be checked against the CRON expression and the time the import was last run.
- Source Path (string) - the folder that files will be imported from
- CRON Expression (string) - a user-provided CRON string that determines when the import will run. Using "seconds" in the expression is not supported since there is no guarantee the node will be checked at a specific second.
- Generate (button) - Opens a new window to CRON tab guru that will help the designer easily create a desired CRON expression. If an expression is already configured, the site will open with the currently set expression for more convenient viewing/editing.

## Delete Document

GlobalAction is often used for managing retention schedules, but it currently has no native "delete document" ability. Instead, documents are moved to a "to be deleted" archive where they are later deleted manually in batches. This node will allow you to skip that process and provide automated document deletion.

## Delete Page Range

Deleting document pages is a vital workflow function that can be useful in a variety of scenarios. Currently users have the ability to remove blank pages, barcode pages, or all pages in a document. There are additional cases where deleting specific pages of a document that do not meet this criteria may still be necessary. This node fulfills that requirement.

### Delete Page Range - Configuration Options

- Pages (string) - a comma-delimited selection of pages to be deleted from the process document.

## Inbox Import

Currently, the only way to import a document into a GlobalAction workflow is for it to already exist within the GlobalSearch database. By allowing imports directly from an Inbox, we can allow new documents to immediately enter into a GlobalAction process without needing to be imported into an Archive as part of a separate Capture process.

### Inbox Import - Configuration Options

- Source Inbox (dropdown selection) - the inbox folder that files will be imported from
- Destination Archive (dropdown selection) - the archive folder that files will be imported into
