resource "aws_iam_role_policy_attachment" "ecs_task_execution_role_policy" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# Add Secrets Manager access policy for task execution role
resource "aws_iam_policy" "ecs_task_execution_secrets_access" {
  name        = "${var.environment}-ecs-task-execution-secrets-access"
  description = "Allow ECS task execution role to access secrets"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue"
        ]
        Resource = [
          aws_secretsmanager_secret.keycloak.arn,
          aws_secretsmanager_secret.ecs_fmflow.arn
        ]
      }
    ]
  })

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution_secrets_access" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = aws_iam_policy.ecs_task_execution_secrets_access.arn
}

# CloudWatch Logs Policy for ECS Tasks
resource "aws_iam_policy" "ecs_cloudwatch_logs" {
  name        = "${var.environment}-ecs-cloudwatch-logs"
  description = "Allow ECS tasks to write to CloudWatch logs"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogStream",
          "logs:PutLogEvents",
          "logs:CreateLogGroup",
          "logs:DescribeLogStreams"
        ]
        Resource = [
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/ecs/${var.environment}-api:*",
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/ecs/${var.environment}-api:*:*"
        ]
      }
    ]
  })

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Attach CloudWatch Logs Policy to ECS Task Role
resource "aws_iam_role_policy_attachment" "ecs_cloudwatch_logs" {
  role       = aws_iam_role.ecs_task_role.name
  policy_arn = aws_iam_policy.ecs_cloudwatch_logs.arn
}

# Get current AWS account ID
data "aws_caller_identity" "current" {}

# Production ECS tasks security group
resource "aws_security_group" "ecs_tasks" {
  name        = "${var.environment}-ecs-tasks-sg"
  description = "Security group for ECS tasks"
  vpc_id      = aws_vpc.main.id

  tags = {
    Name        = "${var.environment}-ecs-tasks-sg"
    Environment = var.environment
    ManagedBy   = "tofu"
  }

  lifecycle {
    ignore_changes = [ingress, egress]
  }
}

# Add ingress rule to allow ALB to reach ECS tasks on port 8080
resource "aws_security_group_rule" "ecs_tasks_alb_ingress" {
  type                     = "ingress"
  from_port                = 8080
  to_port                  = 8080
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.alb.id
  description              = "Allow traffic from ALB on port 8080"
  security_group_id        = aws_security_group.ecs_tasks.id
}

# Add egress rule to allow ECS tasks to access AWS Services (Secrets Manager, CloudWatch, etc.)
resource "aws_security_group_rule" "ecs_tasks_https_egress" {
  type              = "egress"
  from_port         = 443
  to_port           = 443
  protocol          = "tcp"
  cidr_blocks       = ["0.0.0.0/0"]
  description       = "Allow HTTPS outbound to AWS Services (Secrets Manager, CloudWatch, etc.)"
  security_group_id = aws_security_group.ecs_tasks.id
}

# Add egress rule to allow ECS tasks to access RDS
resource "aws_security_group_rule" "ecs_tasks_rds_egress" {
  type                     = "egress"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.rds.id
  description              = "Allow PostgreSQL access to RDS"
  security_group_id        = aws_security_group.ecs_tasks.id
}

resource "aws_secretsmanager_secret_version" "keycloak" {
  secret_id = aws_secretsmanager_secret.keycloak.id
  secret_string = var.keycloak_secret
}

# Update ECS Task Role to allow access to the secret
resource "aws_iam_policy" "ecs_secrets_access" {
  name        = "${var.environment}-ecs-secrets-access"
  description = "Allow ECS tasks to access secrets"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue"
        ]
        Resource = [
          aws_secretsmanager_secret.keycloak.arn,
          aws_secretsmanager_secret.ecs_fmflow.arn
        ]
      }
    ]
  })

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

resource "aws_iam_role_policy_attachment" "ecs_secrets_access" {
  role       = aws_iam_role.ecs_task_role.name
  policy_arn = aws_iam_policy.ecs_secrets_access.arn
}

