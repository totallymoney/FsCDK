# FsCDK Documentation Diagram Specifications

This document provides detailed specifications for creating diagrams to enhance FsCDK documentation.

## 1. Multi-Tier Architecture Diagram

**Filename:** `multi-tier-architecture.png`  
**Dimensions:** 1200px × 800px  
**Style:** Clean, modern AWS architecture diagram with soft colors and clear labels

### Visual Elements

**Layout:** Top-to-bottom flow with three horizontal security zones:
1. **Public Internet Zone** (top, light blue background)
2. **VPC Public Subnet Zone** (middle, light green background)
3. **VPC Private Subnet Zone** (bottom, light gray background)

### Components to Illustrate

#### Layer 1: Internet/CDN (Top)
- **Internet** (cloud icon at very top)
- ↓ (arrow)
- **CloudFront CDN** 
  - AWS CloudFront icon
  - Label: "CloudFront Distribution"
  - Badge: "HTTPS/TLS 1.2+"
  - Color: Orange

#### Layer 2: VPC Boundary
- **VPC Rectangle** enclosing all remaining components
  - Border: Dashed line, dark gray
  - Label: "VPC (10.0.0.0/16)"
  - Badge: "2 Availability Zones"

#### Layer 3: Public Subnet (within VPC)
- Background: Light green
- **Internet Gateway**
  - AWS IGW icon
  - Label: "Internet Gateway"
  - Connected to CloudFront
  
- **Application Load Balancer (ALB)**
  - AWS ALB icon
  - Label: "Application Load Balancer"
  - Badge: "Multi-AZ, HTTPS"
  - Color: Purple
  - Placed in 2 AZs (AZ-1, AZ-2)

#### Layer 4: Private Subnet (within VPC)
- Background: Light gray
- **NAT Gateway**
  - AWS NAT Gateway icon
  - Label: "NAT Gateway"
  - Color: Blue
  
- **Lambda Functions**
  - AWS Lambda icon (×3 to show multiple)
  - Label: "Lambda Functions (Node.js)"
  - Badge: "Auto-scaling"
  - Color: Orange
  - Security group boundary (dotted rectangle)
  
- **RDS Database**
  - AWS RDS PostgreSQL icon
  - Label: "RDS PostgreSQL (Multi-AZ)"
  - Badge: "Encrypted, Private"
  - Color: Blue
  - Security group boundary (dotted rectangle)
  - Show read replica in AZ-2

#### Layer 5: Additional Services (Side boxes)
- **S3 Bucket**
  - AWS S3 icon
  - Label: "S3 Static Assets"
  - Badge: "Encrypted, Versioned"
  - Color: Green
  - Connected via dashed line to CloudFront and Lambda

- **Cognito**
  - AWS Cognito icon
  - Label: "Cognito User Pool"
  - Badge: "Authentication"
  - Color: Red
  - Connected via dashed line to ALB

### Data Flow Arrows

1. **User Request Flow** (solid blue arrows):
   - Internet → CloudFront → IGW → ALB → Lambda → RDS

2. **Static Content Flow** (dashed green arrows):
   - S3 → CloudFront

3. **Authentication Flow** (dotted red arrows):
   - User → CloudFront → ALB → Cognito

4. **Outbound Traffic** (solid gray arrows):
   - Lambda → NAT Gateway → IGW → Internet

### Security Annotations

- **Security Group 1** (Lambda):
  - Dotted rectangle around Lambda
  - Label: "SG-Lambda"
  - Rules: "Inbound from ALB only"

- **Security Group 2** (RDS):
  - Dotted rectangle around RDS
  - Label: "SG-Database"
  - Rules: "Inbound from Lambda SG only"

### Legend (Bottom Right)
```
┌─────────────────────────────┐
│ LEGEND                      │
├─────────────────────────────┤
│ ─────> Request Flow         │
│ ┄┄┄┄┄> Static Content       │
│ ····> Authentication        │
│ ▢ Security Group            │
│ ▭ Availability Zone         │
└─────────────────────────────┘
```

### Text Labels

**Title:** "Multi-Tier Web Application Architecture"  
**Subtitle:** "Production-ready with security best practices and high availability"

### Color Palette
- VPC Border: #232F3E (AWS Dark)
- Public Subnet: #E8F5E9 (Light Green)
- Private Subnet: #F5F5F5 (Light Gray)
- Arrows: #0073BB (AWS Blue)
- Security Groups: #FF9900 (AWS Orange), dashed border
- Icons: Official AWS service colors

### Export Format
- PNG with transparent background
- 300 DPI for documentation
- Optimized for web (~200KB max)

---

## 2. DynamoDB Single-Table Design Visual

**Filename:** `dynamodb-single-table.png`  
**Dimensions:** 1000px × 700px  
**Style:** Clean table visualization with color-coded entity types

### Visual Structure

**Layout:** Two-part visualization:
1. **Top Half:** Actual table data (how it's stored)
2. **Bottom Half:** Access pattern diagrams (how it's queried)

### Part 1: Table Data Visualization

**Title:** "Single-Table Design: Users + Orders"

Create a table with these columns and styling:

#### Table Header
```
┌──────────────────┬────────────────────┬──────────┬─────────────────────────────┐
│ PK (Partition)   │ SK (Sort Key)      │ Type     │ Attributes                  │
└──────────────────┴────────────────────┴──────────┴─────────────────────────────┘
```

#### Table Rows (Color-coded by entity type)

**User Entities** (Light Blue Background: #E3F2FD):
```
│ USER#alice       │ METADATA           │ User     │ name: "Alice", email: "a@…" │
```

**Order Entities** (Light Green Background: #E8F5E9):
```
│ USER#alice       │ ORDER#2024-001     │ Order    │ items: […], total: $99.00   │
│ USER#alice       │ ORDER#2024-002     │ Order    │ items: […], total: $150.00  │
│ USER#alice       │ ORDER#2024-003     │ Order    │ items: […], total: $42.50   │
```

**User Entities** (Light Blue Background):
```
│ USER#bob         │ METADATA           │ User     │ name: "Bob", email: "b@…"   │
```

**Order Entities** (Light Green Background):
```
│ USER#bob         │ ORDER#2024-004     │ Order    │ items: […], total: $200.00  │
```

**Inverted Index (for GSI)** (Light Orange Background: #FFF3E0):
```
│ ORDER#2024-001   │ USER#alice         │ OrderInv │ timestamp: "2024-01-15"     │
│ ORDER#2024-002   │ USER#alice         │ OrderInv │ timestamp: "2024-02-20"     │
│ ORDER#2024-003   │ USER#alice         │ OrderInv │ timestamp: "2024-03-10"     │
│ ORDER#2024-004   │ USER#bob           │ OrderInv │ timestamp: "2024-03-15"     │
```

### Part 2: Access Pattern Queries

Show 3 query patterns with visual arrows:

#### Query 1: Get User and All Their Orders
```
Query: PK = "USER#alice"
       SK begins_with "O"

Result: [arrow pointing to]
┌─ USER#alice → ORDER#2024-001
├─ USER#alice → ORDER#2024-002  
└─ USER#alice → ORDER#2024-003

Benefits: Single query, efficient
```

#### Query 2: Get Single Order Details
```
Query: PK = "USER#alice"
       SK = "ORDER#2024-001"

Result: [arrow pointing to]
└─ ORDER#2024-001 (single item)

Benefits: O(1) access
```

#### Query 3: List All Orders (using GSI)
```
GSI Query: PK = "ORDER#2024-001"
           
Result: [arrow pointing to inverted index rows]
└─ All orders sorted by timestamp

Benefits: Different access pattern, same table
```

### Annotations

**Key Concepts** (callout boxes):

1. **Composite Keys** (top right):
   ```
   PK: Entity Identifier
   SK: Attribute + Value
   Enables hierarchical queries
   ```

2. **Benefits** (bottom left):
   ```
   ✓ One table, multiple entities
   ✓ Efficient queries (no joins)
   ✓ Cost-effective (fewer RCUs)
   ✓ Flexible schema
   ```

3. **GSI Note** (bottom right):
   ```
   GSI1: Inverted PK/SK
   Enables "reverse" lookups
   Sparse index (only orders)
   ```

### Visual Style
- Table borders: 1px solid #BDBDBD
- Header: Dark gray background (#424242), white text
- Alternating row colors for readability
- Monospace font for keys (Consolas, 14px)
- Sans-serif font for descriptions (Arial, 12px)
- Arrows: Bold blue (#1976D2), 3px width

### Legend
```
┌────────────────────────────┐
│ ENTITY TYPES               │
├────────────────────────────┤
│ █ User Metadata            │
│ █ Order Records            │
│ █ Inverted Index (GSI)     │
└────────────────────────────┘
```

### Export Format
- PNG with white background
- 300 DPI
- Optimized for web (~150KB max)

---

## 3. Video Embed (No Diagram Needed)

For the Rick Houlihan video, we'll use a standard YouTube embed with custom styling.

**Implementation:** HTML iframe in F# documentation comment block

---

## Design Tools Recommendations

**For Multi-Tier Architecture:**
- **Cloudcraft** (https://cloudcraft.co) - AWS-specific, clean diagrams
- **Draw.io / Diagrams.net** - Free, AWS icon library available
- **Lucidchart** - Professional, AWS templates
- **Excalidraw** - Hand-drawn style, modern

**For DynamoDB Table:**
- **Figma** - Best for table layouts with precise control
- **Google Slides** - Quick table creation, export as PNG
- **HTML/CSS** - Generate table programmatically, screenshot
- **Excel/Numbers** - Create table, style, export as image

**For AI Generation:**
If using AI image generation (DALL-E, Midjourney, Stable Diffusion):
- Focus on "clean technical diagram" style
- Use "AWS architecture diagram" as base prompt
- Request "flat design, pastel colors, clear labels"
- Generate at 2x resolution, downscale for web

---

## Accessibility Requirements

All diagrams must include:
- High contrast colors (WCAG AA compliant)
- Clear, readable labels (minimum 12px font)
- Alt text descriptions in markdown
- SVG format when possible (scalable)
- PNG fallback for complex diagrams

---

**Last Updated:** November 8, 2025  
**Maintained By:** FsCDK Documentation Team
