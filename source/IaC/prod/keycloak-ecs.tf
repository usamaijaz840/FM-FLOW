# Production Keycloak admin secret
resource "aws_secretsmanager_secret" "keycloak_admin" {
  name        = "${var.environment}-keycloak-admin"
  description = "Keycloak admin credentials for ${var.environment} environment"

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

resource "aws_secretsmanager_secret_version" "keycloak_admin" {
  secret_id = aws_secretsmanager_secret.keycloak_admin.id
  secret_string = jsonencode({
    username = var.keycloak_admin_username
    password = var.keycloak_admin_password
  })
}

# Production Keycloak database secret
resource "aws_secretsmanager_secret" "keycloak_db" {
  name        = "${var.environment}-keycloak-database"
  description = "Keycloak database connection for ${var.environment} environment"

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

resource "aws_secretsmanager_secret_version" "keycloak_db" {
  secret_id = aws_secretsmanager_secret.keycloak_db.id
  secret_string = jsonencode({
    host     = aws_db_instance.postgresql.address
    port     = tostring(var.db_port)
    database = var.keycloak_db_name
    username = var.db_username
    password = var.db_password
    url      = "jdbc:postgresql://${aws_db_instance.postgresql.address}:${var.db_port}/${var.keycloak_db_name}"
  })
}

# Production CloudWatch Log Group for Keycloak
resource "aws_cloudwatch_log_group" "ecs_keycloak" {
  name              = "/ecs/${var.environment}-keycloak"
  retention_in_days = 30

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Production Keycloak ECS Task Definition
resource "aws_ecs_task_definition" "keycloak" {
  family                   = "${var.environment}-keycloak"
  network_mode            = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                     = 512
  memory                  = 1024
  execution_role_arn      = aws_iam_role.ecs_task_execution_role.arn
  task_role_arn           = aws_iam_role.ecs_task_role.arn

  container_definitions = jsonencode([
    {
      name      = "keycloak"
      image     = "quay.io/keycloak/keycloak:latest"
      essential = true
      portMappings = [
        {
          containerPort = 8080
          hostPort      = 8080
          protocol      = "tcp"
        }
      ]
      environment = [
        {
          name  = "KC_HOSTNAME_STRICT_BACKCHANNEL"
          value = "false"
        },
        {
          name  = "KC_HTTP_ENABLED"
          value = "true"
        },
        {
          name  = "KC_HEALTH_ENABLED"
          value = "true"
        },
        {
          name  = "KC_METRICS_ENABLED"
          value = "true"
        },
        {
          name  = "KC_DB"
          value = "postgres"
        },
        {
          name  = "KC_PROXY"
          value = "edge"
        },
        {
          name  = "KC_PROXY_HEADERS"
          value = "xforwarded"
        },
        {
          name  = "KC_HTTP_RELATIVE_PATH"
          value = "/"
        },
        {
          name  = "KC_HOSTNAME"
          value = "identity.prod.referralsource-qa.com"
        },
        {
          name  = "KC_HOSTNAME_STRICT"
          value = "false"
        },
        {
          name  = "KC_CACHE"
          value = "local"
        }
      ]
      secrets = [
        {
          name      = "KC_DB_URL_HOST"
          valueFrom = "${aws_secretsmanager_secret.keycloak_db.arn}:host::"
        },
        {
          name      = "KC_DB_URL_PORT"
          valueFrom = "${aws_secretsmanager_secret.keycloak_db.arn}:port::"
        },
        {
          name      = "KC_DB_URL_DATABASE"
          valueFrom = "${aws_secretsmanager_secret.keycloak_db.arn}:database::"
        },
        {
          name      = "KC_DB_USERNAME"
          valueFrom = "${aws_secretsmanager_secret.keycloak_db.arn}:username::"
        },
        {
          name      = "KC_DB_PASSWORD"
          valueFrom = "${aws_secretsmanager_secret.keycloak_db.arn}:password::"
        },
        {
          name      = "KEYCLOAK_ADMIN"
          valueFrom = "${aws_secretsmanager_secret.keycloak_admin.arn}:username::"
        },
        {
          name      = "KEYCLOAK_ADMIN_PASSWORD"
          valueFrom = "${aws_secretsmanager_secret.keycloak_admin.arn}:password::"
        }
      ]
      command = ["start"]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = "/ecs/${var.environment}-keycloak"
          "awslogs-region"        = var.aws_region
          "awslogs-stream-prefix" = "ecs"
        }
      }
    }
  ])

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Production Keycloak ECS Service
resource "aws_ecs_service" "keycloak" {
  name            = "${var.environment}-keycloak-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.keycloak.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = [aws_subnet.public.id, aws_subnet.public_2.id]
    security_groups  = [aws_security_group.ecs_tasks.id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.keycloak.arn
    container_name   = "keycloak"
    container_port   = 8080
  }

  depends_on = [
    aws_cloudwatch_log_group.ecs_keycloak,
    aws_lb_target_group.keycloak
  ]

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Production Target Group for Keycloak
resource "aws_lb_target_group" "keycloak" {
  name        = "${var.environment}-keycloak-tg"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = aws_vpc.main.id
  target_type = "ip"

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200,302,404"
    path                = "/"
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 10
    unhealthy_threshold = 5
  }

  lifecycle {
    create_before_destroy = true
  }

  tags = {
    Name        = "${var.environment}-keycloak-tg"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# ALB Listener Rule for Keycloak - Route by host header
resource "aws_lb_listener_rule" "keycloak_host" {
  listener_arn = aws_lb_listener.https.arn
  priority     = 50

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.keycloak.arn
  }

  condition {
    host_header {
      values = ["identity.${var.domain_name}"]
    }
  }

  depends_on = [aws_lb_target_group.keycloak]
}

# ALB Listener Rule for Keycloak - Route by path pattern
resource "aws_lb_listener_rule" "keycloak_path" {
  listener_arn = aws_lb_listener.https.arn
  priority     = 100

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.keycloak.arn
  }

  condition {
    path_pattern {
      values = ["/auth/*", "/keycloak/*", "/realms/*"]
    }
  }

  depends_on = [aws_lb_target_group.keycloak]
}

# Update IAM policy to allow Keycloak secrets access
resource "aws_iam_policy" "ecs_task_execution_keycloak_secrets" {
  name        = "${var.environment}-ecs-task-execution-keycloak-secrets"
  description = "Allow ECS task execution role to access Keycloak secrets"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue"
        ]
        Resource = [
          aws_secretsmanager_secret.keycloak_admin.arn,
          aws_secretsmanager_secret.keycloak_db.arn
        ]
      }
    ]
  })

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution_keycloak_secrets" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = aws_iam_policy.ecs_task_execution_keycloak_secrets.arn
}

# Outputs for Keycloak
output "keycloak_url" {
  description = "URL to access Keycloak admin console (AWS Route53)"
  value       = var.aws_keycloak_auth_server_url != "" ? "${var.aws_keycloak_auth_server_url}/auth/admin" : "https://identity.${var.domain_name}/auth/admin"
}

output "keycloak_health_url" {
  description = "URL to check Keycloak health (AWS Route53)"
  value       = var.aws_keycloak_auth_server_url != "" ? "${var.aws_keycloak_auth_server_url}/health/ready" : "https://identity.${var.domain_name}/health/ready"
}

output "keycloak_realm_url" {
  description = "URL to access Keycloak realm (AWS Route53)"
  value       = var.aws_keycloak_auth_server_url != "" ? "${var.aws_keycloak_auth_server_url}/realms/${var.keycloak_realm}" : "https://identity.${var.domain_name}/realms/${var.keycloak_realm}"
}

output "keycloak_external_url" {
  description = "External/existing Keycloak URL"
  value       = var.keycloak_auth_server_url
}

output "keycloak_aws_url" {
  description = "AWS-hosted Keycloak URL (Route53)"
  value       = var.aws_keycloak_auth_server_url != "" ? var.aws_keycloak_auth_server_url : "https://identity.${var.domain_name}"
}

