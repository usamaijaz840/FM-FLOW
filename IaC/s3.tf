# S3 bucket for application data
resource "aws_s3_bucket" "app_data" {
  bucket = "${var.environment}-fm-flow-data"

  tags = {
    Name        = "${var.environment}-fm-flow-data"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# S3 bucket versioning
resource "aws_s3_bucket_versioning" "app_data" {
  bucket = aws_s3_bucket.app_data.id
  versioning_configuration {
    status = "Enabled"
  }
}

# S3 bucket server-side encryption
resource "aws_s3_bucket_server_side_encryption_configuration" "app_data" {
  bucket = aws_s3_bucket.app_data.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# S3 bucket public access block
resource "aws_s3_bucket_public_access_block" "app_data" {
  bucket = aws_s3_bucket.app_data.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# S3 bucket lifecycle configuration
resource "aws_s3_bucket_lifecycle_configuration" "app_data" {
  bucket = aws_s3_bucket.app_data.id

  rule {
    id     = "cleanup-old-versions"
    status = "Enabled"

    filter {
      prefix = ""  # Apply to all objects in the bucket
    }

    noncurrent_version_expiration {
      noncurrent_days = 90
    }
  }
}

# S3 Access Point for the data bucket
resource "aws_s3_access_point" "app_data" {
  name   = "${var.environment}-fm-flow-data-ap"
  bucket = aws_s3_bucket.app_data.id

  public_access_block_configuration {
    block_public_acls       = true
    block_public_policy     = true
    ignore_public_acls      = true
    restrict_public_buckets = true
  }
}

# Update the ECS S3 access policy to use the access point
resource "aws_iam_policy" "ecs_s3_access" {
  name        = "${var.environment}-ecs-s3-access"
  description = "Allow ECS tasks to access S3 bucket through access point"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:PutObject",
          "s3:GetObject",
          "s3:DeleteObject",
          "s3:ListBucket"
        ]
        Resource = [
          aws_s3_bucket.app_data.arn,
          "${aws_s3_bucket.app_data.arn}/*",
          aws_s3_access_point.app_data.arn,
          "${aws_s3_access_point.app_data.arn}/object/*"
        ]
      }
    ]
  })

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

resource "aws_iam_role_policy_attachment" "ecs_s3_access" {
  role       = aws_iam_role.ecs_task_role.name
  policy_arn = aws_iam_policy.ecs_s3_access.arn
}

# S3 bucket for React app static hosting
resource "aws_s3_bucket" "react_app" {
  bucket = "${var.environment}-fm-flow-web"

  tags = {
    Name        = "${var.environment}-fm-flow-web"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# S3 bucket website configuration
resource "aws_s3_bucket_website_configuration" "react_app" {
  bucket = aws_s3_bucket.react_app.id

  index_document {
    suffix = "index.html"
  }

  error_document {
    key = "index.html"
  }
}

# S3 bucket policy for CloudFront access
resource "aws_s3_bucket_policy" "react_app" {
  bucket = aws_s3_bucket.react_app.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "AllowCloudFrontServicePrincipal"
        Effect    = "Allow"
        Principal = {
          Service = "cloudfront.amazonaws.com"
        }
        Action   = "s3:GetObject"
        Resource = "${aws_s3_bucket.react_app.arn}/*"
        Condition = {
          StringEquals = {
            "AWS:SourceArn" = aws_cloudfront_distribution.react_app.arn
          }
        }
      }
    ]
  })

  depends_on = [aws_cloudfront_distribution.react_app]
}
