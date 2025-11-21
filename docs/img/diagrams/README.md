# FsCDK Documentation Diagrams

This directory contains both the source Mermaid diagrams (`.mmd` files) and the generated SVG images (`.svg` files) used in the FsCDK documentation.

## Generated SVG Files ✓

All 7 diagrams have been successfully converted to SVG format:

| File | Size | Description |
|------|------|-------------|
| `cognito-oauth-m2m-flow.svg` | 29 KB | OAuth 2.0 Machine-to-Machine authentication sequence diagram |
| `eventbridge-architecture.svg` | 23 KB | EventBridge event-driven architecture flow diagram |
| `step-functions-order-workflow.svg` | 394 KB | Step Functions order processing state diagram |
| `lambda-production-defaults.svg` | 30 KB | Lambda production architecture with FsCDK defaults |
| `fscdk-architecture.svg` | 20 KB | FsCDK architecture overview |
| `sns-sqs-messaging-patterns.svg` | 32 KB | SNS/SQS messaging patterns diagram |
| `multi-tier-architecture.svg` | 39 KB | Multi-tier application architecture |

**Total:** 567 KB (7 files)

## Source Files

All diagrams have corresponding `.mmd` source files that can be edited and regenerated:

- `cognito-oauth-m2m-flow.mmd`
- `eventbridge-architecture.mmd`
- `step-functions-order-workflow.mmd`
- `lambda-production-defaults.mmd`
- `fscdk-architecture.mmd`
- `sns-sqs-messaging-patterns.mmd`
- `multi-tier-architecture.mmd`

## Editing Diagrams

To modify a diagram:

1. Edit the corresponding `.mmd` file
2. Run the conversion command:
   ```bash
   mmdc -i diagram-name.mmd -o diagram-name.svg
   ```
3. The updated SVG will be automatically used in the documentation

## Regenerating All Diagrams

If you need to regenerate all SVG files:

**Windows (PowerShell):**
```powershell
cd C:\git\FsCDK\docs
.\convert-mermaid-to-svg.ps1
```

**Linux/Mac (Bash):**
```bash
cd /path/to/FsCDK/docs
./convert-mermaid-to-svg.sh
```

## Documentation References

These SVG files are referenced in the following documentation files:

| Documentation File | Diagram Used |
|-------------------|--------------|
| `cognito-m2m-oauth.fsx` | `cognito-oauth-m2m-flow.svg` |
| `eventbridge.fsx` | `eventbridge-architecture.svg` |
| `step-functions.fsx` | `step-functions-order-workflow.svg` |
| `lambda-production-defaults.fsx` | `lambda-production-defaults.svg` |
| `index.fsx` | `fscdk-architecture.svg` |
| `sns-sqs-messaging.fsx` | `sns-sqs-messaging-patterns.svg` |
| `multi-tier-example.fsx` | `multi-tier-architecture.svg` |

## Tools

- **Mermaid CLI**: Converts `.mmd` files to `.svg`
- **Installation**: `npm install -g @mermaid-js/mermaid-cli`
- **Documentation**: https://github.com/mermaid-js/mermaid-cli

## Benefits of SVG Format

✅ Fast page load (no JavaScript rendering)
✅ Better SEO (search engines can index images)
✅ Works offline (no CDN dependency)
✅ Scalable (SVG is vector-based)
✅ Version control friendly
✅ Cross-browser compatible

---

**Last Updated:** 2025-11-19
**Total Diagrams:** 7
**Status:** ✅ All diagrams generated successfully
