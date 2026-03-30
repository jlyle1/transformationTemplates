# FHIR to PIQI Transformation Learning Plan

## Phase 1: Running the Converter

### Option A: Console App (Current Setup)
```bash
cd src/FhirConverter
dotnet run
```
The `Program.cs` has a hardcoded sample FHIR Bundle. Edit it to test different inputs.

### Option B: CLI Tool (from FHIR-Converter)
```bash
cd lib/FHIR-Converter/src/Microsoft.Health.Fhir.Liquid.Converter.Tool
dotnet run -- convert -d ../../../data/Templates -r FHIRToPIQI -t BundleJSON -c input.json
```

### Option C: Web API / Swagger
The FHIR-Converter can run as an API:
```bash
cd lib/FHIR-Converter
docker build -t fhir-converter .
docker run -p 8080:8080 fhir-converter
```
Then access Swagger at `http://localhost:8080/swagger`

### Recommended: Start with Console App
Easiest for rapid iteration - edit `Program.cs`, run, see output.

---

## Phase 2: Liquid Template Basics

### Core Concepts
| Concept | Syntax | Example |
|---------|--------|---------|
| Output variable | `{{ var }}` | `{{ msg.id }}` |
| Filter | `{{ var \| filter }}` | `{{ msg.name \| first }}` |
| Conditional | `{% if %}...{% endif %}` | `{% if msg.birthDate %}...{% endif %}` |
| Loop | `{% for item in array %}` | `{% for entry in msg.entry %}` |
| Include | `{% include 'template' %}` | `{% include '_patient' msg: resource %}` |
| Assign | `{% assign var = value %}` | `{% assign patId = msg.id %}` |

### Template Flow in This Project
```
BundleJSON.liquid          <- Root template (entry point)
  └── _bundle.liquid       <- Extracts resources from Bundle
        ├── _patient.liquid
        ├── _allergyintolerance.liquid
        ├── _medication.liquid
        │     └── _medInstruction.liquid
        │     └── _medDosageInstruction.liquid
        └── _codableconcept.liquid  <- Reusable helper
```

### Key Files to Study
1. `FHIRToPIQI/BundleJSON.liquid` - See how root template works
2. `FHIRToPIQI/_bundle.liquid` - See how resources are extracted and dispatched
3. `FHIRToPIQI/_codableconcept.liquid` - See reusable pattern for CodeableConcept

### FHIR Converter Custom Filters
The converter adds custom Liquid filters beyond standard Liquid:
- `| to_json_string` - Convert object to JSON
- `| first` - Get first element of array
- `| match: 'regex'` - Regex matching
- `| split: ','` - Split string
- `| date: 'format'` - Date formatting

---

## Phase 3: Template Organization

### Current Structure
```
FHIRToPIQI/
├── metadata.json          <- Required: defines data type
├── BundleJSON.liquid      <- Root template (referenced in code)
├── _bundle.liquid         <- Partial (starts with _)
├── _patient.liquid
├── _medication.liquid
└── _codableconcept.liquid
```

### Naming Conventions
| Pattern | Purpose |
|---------|---------|
| `ResourceType.liquid` | Root template (no underscore) |
| `_resourcetype.liquid` | Partial template (included by others) |
| `_helper.liquid` | Reusable helper (e.g., `_codableconcept`) |

### Extension Strategy Options

**Option 1: Modify Existing Templates (Current Approach)**
- Pros: Simple, all in one place
- Cons: Harder to merge upstream changes

**Option 2: Create Override Folder**
```
FHIRToPIQI/
├── base/                  <- Original templates
└── va_emi/                <- Your extensions
    └── _medication.liquid <- Overrides base version
```
Would require modifying template loading logic.

**Option 3: Separate Template Set**
```
FHIRToPIQI/                <- Base templates
VA_EMI_ToPIQI/             <- New template set with includes
├── metadata.json
├── BundleJSON.liquid
└── _medication.liquid     <- Can include from FHIRToPIQI
```

**Recommendation:** For now, modify existing templates directly (Option 1). They're already in your repo, not a submodule. The submodule (`lib/FHIR-Converter`) is just the converter engine, not the templates.

---

## Phase 4: Simple Test Change

### Goal: Add `orderStatus` to medications (simple string)

#### Step 1: Find the template
```
FHIRToPIQI/_medication.liquid
```

#### Step 2: Identify where to add
Look for existing fields like `"medication":` or `"startDate":` and add nearby.

#### Step 3: Add the field
```liquid
"orderStatus": {
  "codings": [
    {
      "code": "{{ med.status }}"
    }
  ],
  "text": "{{ med.status }}"
},
```

#### Step 4: Test with sample data
Update `Program.cs` to include a MedicationRequest with `"status": "active"`.

#### Step 5: Run and verify
```bash
cd src/FhirConverter
dotnet run
```

---

## Phase 5: Progressive Complexity

### Level 1: Simple String Fields
```liquid
"prescriptionNumber": "{{ med.identifier[0].value }}",
"remainingFills": "{{ med.dispenseRequest.numberOfRepeatsAllowed }}",
```

### Level 2: Multipart Elements (CodeableConcept)
```liquid
"sourceCategory": {
  {% if med.category[0] %}
  {% include '_codableconcept' cc: med.category[0] -%}
  {% endif %}
},
```

### Level 3: Conditional Elements
```liquid
{% if med.extension %}
  {% for ext in med.extension %}
    {% if ext.url contains 'patientCounseled' %}
"patientCounseled": {{ ext.valueBoolean }},
    {% endif %}
  {% endfor %}
{% endif %}
```

### Level 4: Navigating Across Resources
This is the hardest - requires bundle context.

```liquid
{% comment %}
  To resolve a reference like "Practitioner/123",
  you need to find it in the bundle entries
{% endcomment %}

{% assign requesterRef = med.requester.reference %}
{% if requesterRef %}
  {% for entry in bundle.entry %}
    {% assign fullUrl = entry.fullUrl | split: '/' | last %}
    {% assign refId = requesterRef | split: '/' | last %}
    {% if fullUrl == refId %}
      {% assign practitioner = entry.resource %}
"orderingProviderName": "{{ practitioner.name[0].given[0] }} {{ practitioner.name[0].family }}",
    {% endif %}
  {% endfor %}
{% endif %}
```

### Level 5: Slices and Discriminators
FHIR profiles use slicing. Example: US Core Patient has sliced `identifier`:

```liquid
{% for id in msg.identifier %}
  {% if id.system == 'http://hl7.org/fhir/sid/us-ssn' %}
"ssn": "{{ id.value }}",
  {% elsif id.system contains 'va.gov' and id.system contains 'icn' %}
"vaIcn": "{{ id.value }}",
  {% endif %}
{% endfor %}
```

---

## Suggested Learning Path

| Day | Task | Outcome |
|-----|------|---------|
| 1 | Run converter, examine output | Understand data flow |
| 2 | Read `_bundle.liquid` and `_patient.liquid` | Understand template structure |
| 3 | Add `orderStatus` (simple string) | First working change |
| 4 | Add `sourceCategory` (CodeableConcept) | Use include pattern |
| 5 | Add extension-based field | Handle conditionals |
| 6 | Add cross-resource reference | Handle bundle navigation |

---

## Quick Reference: FHIR to PIQI Field Mapping

| PIQI Field | FHIR Path | Template Pattern |
|------------|-----------|------------------|
| Simple string | `resource.field` | `"{{ msg.field }}"` |
| Nested string | `resource.parent.child` | `"{{ msg.parent.child }}"` |
| Array first | `resource.array[0]` | `"{{ msg.array[0].value }}"` |
| CodeableConcept | `resource.code` | `{% include '_codableconcept' cc: msg.code %}` |
| Reference resolve | `resource.ref.reference` | Loop through bundle entries |
| Extension | `resource.extension` | Loop and match URL |

---

## Troubleshooting

### Common Issues

1. **Missing comma in output JSON**
   - Liquid outputs exactly what you write
   - Trailing commas cause issues - use conditionals

2. **Blank output**
   - Variable path is wrong
   - Check case sensitivity (`resourceType` not `ResourceType`)

3. **Template not found**
   - Check underscore prefix for partials
   - Check `metadata.json` dataType matches

### Debugging Tips
```liquid
{% comment %} Debug: output raw object {% endcomment %}
"_debug": {{ med | to_json_string }},
```