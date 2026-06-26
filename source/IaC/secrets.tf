locals {
  ecs_fmflow_secret_keys = [
    "ASPNETCORE_URLS",
    "AWS__Profile",
    "AWS__Region",
    "AllowedOrigins__0",
    "App__Frontend",
    "BaseUrl",
    "ConnectionStrings__FMFlowDB",
    "CustomJwt__Audience",
    "CustomJwt__Issuer",
    "CustomJwt__SigningKey",
    "DB_HOST",
    "DB_NAME",
    "DB_PASSWORD",
    "DB_PORT",
    "DB_USER",
    "EmailSettings__ApiKey",
    "EmailSettings__EnableEmailService",
    "EmailSettings__FromEmail",
    "EmailSettings__FromName",
    "FileUpload__MaxFileSize",
    "FileUpload__S3AccessPointArn",
    "FileUpload__S3BucketName",
    "Google__PlacesApiKey",
    "Google__PlacesApiUrl",
    "Google__UseMockPlacesService",
    "Keycloak__RequireHttpsMetadata",
    "Keycloak__auth-server-url",
    "Keycloak__confidential-port",
    "Keycloak__credentials__secret",
    "Keycloak__realm",
    "Keycloak__registration-token",
    "Keycloak__resource",
    "Keycloak__ssl-required",
    "Keycloak__verify-token-audience",
    "MXConnect__ApiKey",
    "MXConnect__ApiSecret",
    "MXConnect__BaseUrl",
    "MXConnect__MerchantId",
    "ReCaptcha__SecretKey",
    "Swagger__ShowSwaggerUI",
    "WebAppBaseUrl"
  ]
}


resource "aws_secretsmanager_secret" "ecs_fmflow" {
  name        = "${var.environment}-ecs-fmflow"
  description = "ECS FMFlow application configuration secrets for ${var.environment} environment"

  tags = {
    Name        = "${var.environment}-ecs-fmflow"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

resource "aws_secretsmanager_secret_version" "ecs_fmflow" {
  secret_id = aws_secretsmanager_secret.ecs_fmflow.id
  secret_string = jsonencode({
    ASPNETCORE_URLS                      = "http://+:8080"
    ConnectionStrings__FMFlowDB         = "Host=${aws_db_instance.postgresql.endpoint};Port=${var.db_port};Database=${var.db_name};Username=${var.db_username};Password=${var.db_password}"
    DB_HOST                              = aws_db_instance.postgresql.endpoint
    DB_PORT                              = tostring(var.db_port)
    DB_NAME                              = var.db_name
    DB_USER                              = var.db_username
    DB_PASSWORD                          = var.db_password
    FileUpload__MaxFileSize              = tostring(var.file_upload_max_size)
    FileUpload__S3BucketName             = aws_s3_bucket.app_data.bucket
    FileUpload__S3AccessPointArn          = aws_s3_access_point.app_data.arn
    AWS__Profile                         = var.aws_profile
    AWS__Region                          = var.aws_region
    EmailSettings__ApiKey                = var.email_api_key
    EmailSettings__FromEmail             = var.email_from_address
    EmailSettings__FromName              = var.email_from_name
    EmailSettings__EnableEmailService    = tostring(var.email_service_enabled)
    Keycloak__auth-server-url            = var.keycloak_auth_server_url
    Keycloak__realm                      = var.keycloak_realm
    Keycloak__resource                   = var.keycloak_resource
    Keycloak__ssl-required               = var.keycloak_ssl_required
    Keycloak__verify-token-audience      = tostring(var.keycloak_verify_token_audience)
    Keycloak__confidential-port          = tostring(var.keycloak_confidential_port)
    Keycloak__RequireHttpsMetadata       = tostring(var.keycloak_require_https_metadata)
    Keycloak__registration-token         = var.keycloak_registration_token
    # Keycloak__credentials__secret        = aws_secretsmanager_secret.keycloak.arn
    Keycloak__credentials__secret        = var.keycloak_secret
    MXConnect__ApiKey                    = var.mx_connect_api_key
    MXConnect__ApiSecret                 = var.mx_connect_api_secret
    MXConnect__BaseUrl                   = var.mx_connect_base_url
    MXConnect__MerchantId                = var.mx_connect_merchant_id
    AllowedOrigins__0                    = var.allowed_origins
    ReCaptcha__SecretKey                 = var.google_recaptcha_site_id
    BaseUrl                              = var.frontend_base_url
    App__Frontend                        = var.integrations_redirect_uri
    WebAppBaseUrl                        = var.frontend_base_url
    Swagger__ShowSwaggerUI               = var.show_swagger_ui ? "true" : "false"
    CustomJwt__SigningKey                = var.jwt_signing_key
    CustomJwt__Audience                  = var.jwt_audience
    CustomJwt__Issuer                    = var.jwt_issuer
    Google__PlacesApiUrl                 = var.google_places_api_url
    Google__PlacesApiKey                 = var.google_places_api_key
    Google__UseMockPlacesService         = tostring(var.use_mock_google_places_api)
  })
}


# Keycloak Secret with updated name
resource "aws_secretsmanager_secret" "keycloak" {
  name        = "${var.environment}/keycloak/credentials-v2"
  description = "Keycloak credentials for ${var.environment} environment"

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}