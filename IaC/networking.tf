# Existing VPC - imported from manually created resource
resource "aws_vpc" "existing" {
  cidr_block           = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = {
    Name        = "${var.environment}-vpc"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Existing public subnet 1 - imported from manually created resource
resource "aws_subnet" "public" {
  vpc_id                  = aws_vpc.existing.id
  cidr_block              = var.public_subnet_cidr
  availability_zone       = var.availability_zone
  map_public_ip_on_launch = true

  tags = {
    Name        = "${var.environment}-public-subnet-1"
    Environment = var.environment
    ManagedBy   = "tofu"
  }

  lifecycle {
    ignore_changes = [cidr_block, availability_zone]
  }
}

# Existing public subnet 2 - imported from manually created resource
resource "aws_subnet" "public_2" {
  vpc_id                  = aws_vpc.existing.id
  cidr_block              = var.public_subnet_cidr_2
  availability_zone       = var.availability_zone_2
  map_public_ip_on_launch = true

  tags = {
    Name        = "${var.environment}-public-subnet-2"
    Environment = var.environment
    ManagedBy   = "tofu"
  }

  lifecycle {
    ignore_changes = [cidr_block, availability_zone]
  }
}

# Existing RDS security group - imported from manually created resource
resource "aws_security_group" "rds" {
  name        = "${var.environment}-rds-sg"
  description = "Security group for RDS PostgreSQL instance"
  vpc_id      = aws_vpc.existing.id

  tags = {
    Name        = "${var.environment}-rds-sg"
    Environment = var.environment
    ManagedBy   = "tofu"
  }

  lifecycle {
    ignore_changes = [ingress, egress]
  }
}

# Add ingress rule to allow ECS tasks to reach RDS on port 5432
resource "aws_security_group_rule" "rds_ecs_ingress" {
  type                     = "ingress"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.ecs_tasks.id
  description              = "Allow PostgreSQL access from ECS tasks"
  security_group_id        = aws_security_group.rds.id
}


# Existing Internet Gateway - imported from manually created resource
resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.existing.id

  tags = {
    Name        = "${var.environment}-igw"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Route Table for Public Subnets
resource "aws_route_table" "public" {
  vpc_id = aws_vpc.existing.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.main.id
  }

  tags = {
    Name        = "${var.environment}-public-rt"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Associate Public Subnet 1 with Route Table
resource "aws_route_table_association" "public" {
  subnet_id      = aws_subnet.public.id
  route_table_id = aws_route_table.public.id
}

# Associate Public Subnet 2 with Route Table
resource "aws_route_table_association" "public_2" {
  subnet_id      = aws_subnet.public_2.id
  route_table_id = aws_route_table.public.id
}


