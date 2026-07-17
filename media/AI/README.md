# AI media and demonstrations

This folder contains the visual assets used to explain the Aerospike LINQPad AI workflow.

## Overview assets

| Path | Purpose |
|---|---|
| `LINQPadAIOV.mp4` | Full AI feature overview video |
| `aerospike_linqpad_ai_features_overview_server_expressions.pptx` | Presentation focused on generated queries and server-side expressions |
| `LINQPadAIOV.tscproj/` | Camtasia project and source assets used to edit the overview video |
| `LINQPadAISummary/` | LINQPad AI provider menu and settings walkthrough |

## Data-model diagrams

`ExDataModel/` contains:

- Sample prompt and canvas screenshots.
- Namespace/set/bin view.
- Customer-invoice document diagrams.
- Complete Chinook-style set ERD.
- Customer-to-track purchase-path ERD.

## Query-generation demonstrations

| Folder | Scenario |
|---|---|
| `ExCustwInvwArtwAlbwTrackPusQuery/` | Query-syntax customer/invoice/artist/album/track purchase query |
| `ExCustwInvwArtwAlbwTrackPusMethod/` | Method-syntax version of the purchase query |
| `ExCustwInvwArtwAlbwTrackPusNative/` | Native Aerospike C# client version |
| `ExQurtywExpressions/` | Driver query using Aerospike expressions |
| `ExNativewExpressions/` | Native client expression generation and execution |
| `ExExplainLinqQuery/` | Explanation of an existing LINQPad query |
| `ExTransformLINQQuerytoNative/` | Translation from driver LINQ to native client code |

Most scenario folders contain request, generated-code, result, and/or short video assets.

## Maintenance

- Keep request screenshots paired with the code and result they produced.
- Record the driver version when regenerating a demonstration.
- Remove or obscure credentials, cluster addresses, and sensitive data.
- Update [`docs/ai-features.md`](../../docs/ai-features.md) when folders or final asset names change.
- Keep large editing intermediates only when they are needed to reproduce the published media.

[Back to the media catalog](../README.md)
