# FMFlow Infrastructure as Code

This directory contains the Infrastructure as Code (IaC) configuration for the FMFlow application using OpenTofu (formerly Terraform).

## Prerequisites

1. Install OpenTofu (formerly Terraform)
   ```bash
   # Windows (using Chocolatey)
   choco install tofu

   # macOS (using Homebrew)
   brew install opentofu
   ```

2. Install AWS CLI and configure credentials
   ```bash
   # Windows (using Chocolatey)
   choco install awscli

   # macOS (using Homebrew)
   brew install awscli

   # Configure AWS credentials
   aws configure --profile fmflow
   ```
   Sign in to the CLI in order to connect to AWS:

   1. Sign into AWS, and search for IAM
   2. Click on the Users link
   3. Click on your account
   4. Click the `Create Access Key` link near the top right
   5. Select CLI, and then check the box by `I understand the above recommendation and want to proceed to create an access key.` Then click `Next`.
   6. Optionally provide a description, and click `Create Access Key`
   7. Either download the .csv or copy both access keys to somewhere you can reference it (you can't view it again so make sure to save it).
   8. Go to a terminal and run `aws configure`. Provide the access key and the secret access key when prompted. Set the default region to `us-east-1`.

3. Install Docker Desktop
   - Download from [Docker's website](https://www.docker.com/products/docker-desktop)
   - Ensure it's running before building containers

4. Run `tofu init`:
```
tofu init -backend-config="bucket=fmflow-terraform-state" \
	-backend-config="key=terraform.tfstate" \
	-backend-config="region=us-east-1"
```

## Infrastructure Management

1. Navigate to the IaC directory
   ```bash
   # Make sure you're in the directory containing main.tf, backend.tf, etc.
   cd D:\Projects\FmFlow\FMFlowRepo\source\IaC
   ```

2. Choose your environment and variable file:
   - For QA environment: `terraform.tfvars` or no variable file
   - For production: `prod.tfvars`
   - For development: `dev.tfvars`

3. Review the planned changes
   ```bash
   # Using a variable file
   tofu plan  # optional -var-file="terraform.tfvars" or prod.tfvars, dev.tfvars

   # Or without a variable file
   tofu plan -var="environment=qa" -var="aws_region=us-east-1"
   ```

4. Apply the infrastructure changes
   ```bash
   # Using a variable file
   tofu apply  # optional -var-file="terraform.tfvars" or prod.tfvars, dev.tfvars

   # Or without a variable file
   tofu apply -var="environment=qa" -var="aws_region=us-east-1"
   ```

## Building and Deploying the API

1. Build the Docker image
   ```bash
   # Navigate to the API project directory
   cd source

   # Build the Docker image
   docker build -f .\FmFlowAPI\FMFlowAPI\Dockerfile -t fm-flow:latest .

   # On Mac a different command is needed, see:
   # https://stackoverflow.com/a/78771115/5199659
   # https://devblogs.microsoft.com/dotnet/improving-multiplatform-container-support/
   # Run the following command:
   docker build --platform=linux/amd64 -f ./FmFlowAPI/FMFlowAPI/Dockerfile_mac -t fm-flow:latest .
   ```

2. Tag and push the image to ECR
   ```bash
   # Get ECR login token
   aws ecr get-login-password --region us-east-1 --profile fmflow | docker login --username AWS --password-stdin 324037278482.dkr.ecr.us-east-1.amazonaws.com

   # Tag the image
   docker tag fm-flow:latest 324037278482.dkr.ecr.us-east-1.amazonaws.com/fm-flow:latest

   # Push to ECR
   docker push 324037278482.dkr.ecr.us-east-1.amazonaws.com/fm-flow:latest
   ```

3. Go to the AWS portal, and force a new deployment of the service.

## Building and Deploying the Web App

The version of NodeJS is 20.15 LTS.

1. Build the React app
   ```bash
   # Install dependencies
   npm install

   # Build the app
   npx nx build FMFlowWeb
   ```

2. Copy the remote `settings.json` to the one in the `dist` folder

3. Upload to S3
   ```bash
   # Upload the build files to S3 (replace bucket name based on environment)
   aws s3 sync dist/apps/FMFlowWeb/ s3://dev-fm-flow-web --profile fmflow  # for QA
   # or
   aws s3 sync dist/ s3://fm-flow-web --profile fmflow     # for production
   ```

4. Add an invalidation to Cloudfront to ensure that all the new files are being served.

## Accessing the Application

After deployment, the application will be available at:
- QA Environment:
  - Web App: https://app.referralsource-qa.com
  - API: https://api.referralsource-qa.com
- Production Environment:
  - Web App: https://app.referralsource.com
  - API: https://api.referralsource.com
- Development Environment:
  - Web App: https://app.dev.referralsource-qa.com
  - API: https://api.dev.referralsource-qa.com

## Infrastructure Components

The infrastructure includes:
- VPC with public subnets
- RDS PostgreSQL database
- ECS Fargate cluster for the API
- Application Load Balancer
- CloudFront distribution for the web app
- S3 buckets for file storage and web hosting
- Route 53 DNS configuration
- ACM SSL certificates

## Maintenance

### Updating Infrastructure
```bash
# Review changes (replace terraform.tfvars with appropriate file)
tofu plan  # optional -var-file="terraform.tfvars" or prod.tfvars, dev.tfvars

# Apply changes
tofu apply  # optional -var-file="terraform.tfvars" or prod.tfvars, dev.tfvars
```

### Destroying Infrastructure
```bash
# Review what will be destroyed
tofu plan -destroy  # optional -var-file="terraform.tfvars" or prod.tfvars, dev.tfvars

# Destroy infrastructure
tofu destroy  # optional -var-file="terraform.tfvars" or prod.tfvars, dev.tfvars
```

## Troubleshooting

1. If the API is not responding:
   - Check ECS service status in AWS Console
   - Verify ALB target group health checks
   - Check CloudWatch logs for the ECS service

2. If the web app is not loading:
   - Verify S3 bucket contents
   - Check CloudFront distribution status
   - Verify DNS records in Route 53

3. If database connection fails:
   - Check RDS instance status
   - Verify security group rules
   - Check VPC connectivity

## Security Notes

- Never commit sensitive information in variable files (*.tfvars)
- Rotate database passwords regularly
- Keep AWS credentials secure
- Monitor CloudWatch logs for security events
- Use different credentials for different environments
