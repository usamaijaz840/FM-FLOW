# Production CloudFront Origin Access Control
resource "aws_cloudfront_origin_access_control" "react_app" {
  name                              = "${var.environment}-react-app-oac-${formatdate("YYYYMMDDHHmmss", timestamp())}"
  description                       = "Origin Access Control for React App"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"

  lifecycle {
    create_before_destroy = true
  }
}

# Production CloudFront distribution
resource "aws_cloudfront_distribution" "react_app" {
  enabled             = true
  is_ipv6_enabled     = true
  default_root_object = "index.html"
  price_class         = "PriceClass_100"

  origin {
    domain_name              = aws_s3_bucket.react_app.bucket_regional_domain_name
    origin_access_control_id = aws_cloudfront_origin_access_control.react_app.id
    origin_id                = "S3-${aws_s3_bucket.react_app.bucket}"
  }

  default_cache_behavior {
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "S3-${aws_s3_bucket.react_app.bucket}"
    viewer_protocol_policy = "redirect-to-https"
    compress               = true

    forwarded_values {
      query_string = false
      cookies {
        forward = "none"
      }
    }

    min_ttl     = 0
    default_ttl = 3600
    max_ttl     = 86400
  }

  custom_error_response {
    error_code         = 403
    response_code      = 200
    response_page_path = "/index.html"
  }

  custom_error_response {
    error_code         = 404
    response_code      = 200
    response_page_path = "/index.html"
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn      = aws_acm_certificate.cert.arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2021"
  }

  aliases = ["app-prod.referralsource-qa.com"]

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Output the CloudFront distribution domain name
output "cloudfront_domain_name" {
  value = aws_cloudfront_distribution.react_app.domain_name
}

