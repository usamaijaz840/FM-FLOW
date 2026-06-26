# React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react/README.md) uses [Babel](https://babeljs.io/) for Fast Refresh
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react-swc) uses [SWC](https://swc.rs/) for Fast Refresh

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type aware lint rules:

- Configure the top-level `parserOptions` property like this:

```js
export default tseslint.config({
  languageOptions: {
    // other options...
    parserOptions: {
      project: ['./tsconfig.node.json', './tsconfig.app.json'],
      tsconfigRootDir: import.meta.dirname,
    },
  },
})
```

- Replace `tseslint.configs.recommended` to `tseslint.configs.recommendedTypeChecked` or `tseslint.configs.strictTypeChecked`
- Optionally add `...tseslint.configs.stylisticTypeChecked`
- Install [eslint-plugin-react](https://github.com/jsx-eslint/eslint-plugin-react) and update the config:

```js
// eslint.config.js
import react from 'eslint-plugin-react'

export default tseslint.config({
  // Set the react version
  settings: { react: { version: '18.3' } },
  plugins: {
    // Add the react plugin
    react,
  },
  rules: {
    // other rules...
    // Enable its recommended rules
    ...react.configs.recommended.rules,
    ...react.configs['jsx-runtime'].rules,
  },
})
```

## Deploying to S3

### Prerequisites
- AWS CLI installed
- AWS credentials configured (`aws configure`)
- S3 bucket created

### Deployment Steps

1. Build the application for production:
```bash
npm run build:prod
```

2. Clear the S3 bucket (optional, but recommended for clean deployments):

For QA:
```bash
aws s3 rm s3://dev-fm-flow-web --recursive --profile fmflow
```

For Production:
```bash
aws s3 rm s3://prod-fm-flow-web --recursive --profile fmflow
```

3. Upload the built files to S3:

For QA:
```bash
aws s3 sync dist/ s3://dev-fm-flow-web --profile fmflow
```

For Production:
```bash
aws s3 sync dist/ s3://prod-fm-flow-web --profile fmflow
```

REMEMBER TO UPDATE THE SETTINGS.JSON FILE WITH THE CORRECT API URL FOR THE ENVIRONMENT YOU ARE DEPLOYING TO.

Deploy the Settings.json file to the S3 bucket.

For QA:
```bash
aws s3 cp Settings.json s3://dev-fm-flow-web/Settings.json --profile fmflow
```

For Production:
```bash
aws s3 cp Settings.json s3://prod-fm-flow-web/Settings.json --profile fmflow
```




### Environment Configuration
The application uses different environment configurations for development and production:

- `.env.development` - Used for local development
- `.env.production` - Used for production builds

Make sure these files are properly configured with the correct API endpoints for each environment.

### Troubleshooting S3 Deployment

If you encounter errors like "InvalidToken" or "AccessDenied", try these steps:

1. Verify your AWS credentials are current:
```bash
aws configure list --profile fmflow
```

2. If needed, update your credentials:
```bash
aws configure --profile fmflow
```

3. Verify you have the correct permissions for the S3 bucket:
```bash
aws s3 ls s3://dev-fm-flow-web --profile fmflow
```

4. If you're using AWS SSO, make sure to login first:
```bash
aws sso login --profile fmflow
```

Common error messages and solutions:
- "InvalidToken": Your AWS credentials have expired or are invalid. Run `aws configure --profile fmflow` to update them.
- "AccessDenied": You don't have the necessary permissions. Contact your AWS administrator.
- "NoSuchBucket": The bucket name is incorrect or doesn't exist in your AWS region. Verify you're using the correct bucket name: `dev-fm-flow-web` for QA or `prod-fm-flow-web` for Production.

## Testing
See the E2E testing guide in `TESTING.md` for setup, commands, and conventions.