# Production DB subnet group
resource "aws_db_subnet_group" "main" {
  name       = "${var.environment}-db-subnet-group"
  subnet_ids = [aws_subnet.public.id, aws_subnet.public_2.id]

  tags = {
    Name        = "${var.environment}-db-subnet-group"
    Environment = var.environment
    ManagedBy   = "tofu"
  }

  lifecycle {
    create_before_destroy = true
    ignore_changes = [subnet_ids]
  }
}

# Production RDS instance
resource "aws_db_instance" "postgresql" {
  identifier             = "${var.environment}-postgresql"
  engine                 = "postgres"
  engine_version         = var.db_engine_version
  instance_class         = var.db_instance_class
  allocated_storage      = var.db_allocated_storage
  storage_type           = "gp2"
  db_name                = var.db_name
  username               = var.db_username
  password               = var.db_password
  port                   = var.db_port
  multi_az              = var.db_multi_az
  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]
  skip_final_snapshot    = var.db_skip_final_snapshot
  backup_retention_period = var.db_backup_retention_period
  publicly_accessible    = false
  deletion_protection    = true

  tags = {
    Name        = "${var.environment}-postgresql"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}


