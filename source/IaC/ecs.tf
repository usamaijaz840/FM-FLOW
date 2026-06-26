# ECS Cluster
resource "aws_ecs_cluster" "main" {
  name = "${var.environment}-cluster"

  tags = {
    Name        = "${var.environment}-ecs-cluster"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# ECS Task Execution Role
resource "aws_iam_role" "ecs_task_execution_role" {
  name = "${var.environment}-ecs-task-execution-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}


# Update ECS Service to use existing security group
resource "aws_ecs_service" "api" {
  name            = "${var.environment}-api-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.api.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = [aws_subnet.public.id, aws_subnet.public_2.id]
    security_groups  = [aws_security_group.ecs_tasks.id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.api.arn
    container_name   = "api"
    container_port   = 8080
  }

  # Add depends_on to ensure log group exists before service starts
  depends_on = [aws_cloudwatch_log_group.ecs_api]

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}


# CloudWatch Log Group
resource "aws_cloudwatch_log_group" "ecs_api" {
  name              = "/ecs/${var.environment}-api"
  retention_in_days = 30

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}