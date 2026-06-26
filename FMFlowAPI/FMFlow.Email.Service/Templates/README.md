# Email templates - developer guide

This folder contains exported SendGrid templates. In Development the API renders these in-repo templates (no SendGrid template_id) and sends via SendGrid transport.

## Preview locally
- Start the API in Development (UseCodeTemplates=true).
- Open: http://localhost:5104/api/EmailTemplatePreview
- Actions:
  - Preview: open rendered HTML
  - Test: send using saved testData from version.json

## Test via API
POST /api/EmailTemplatePreview/send

Example body:
{
  "to": "you@example.com",
  "key": "customer-job-scheduled"
}

Optional: set "from" to override configured FromEmail.

## Export from SendGrid
PowerShell:
$env:SENDGRID_API_KEY="SG.xxxxxx"
npm run export:sendgrid

Output path:
source/FMFlowAPI/FMFlow.Email.Service/Templates/_exported_sendgrid/<templateKey>/<version>/

Files per version: subject.hbs.txt, html.hbs.html, text.hbs.txt, version.json

## Rendering details
- Handlebars.Net renders *.hbs
- CSS is inlined via PreMailer.Net
- Partials:
  - Shared: Templates/_shared/partials/
  - Per-template: <templateKey>/partials/
- Layouts (optional): _layout.hbs.html / _layout.hbs.txt (template-level wins; otherwise _shared/layouts/)

## Switch between code templates and SendGrid
- Dev: UseCodeTemplates=true (render from repo)
- Prod: UseCodeTemplates=false (send by template_id)

## Troubleshooting
- If index is empty, ensure _exported_sendgrid exists.
- If images fail, verify external URLs/CDN reachable.
