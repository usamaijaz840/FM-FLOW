# Security Group for Bastion Host
resource "aws_security_group" "bastion" {
  name        = "${var.environment}-bastion-sg"
  description = "Security group for bastion host to access RDS"
  vpc_id      = aws_vpc.existing.id

  # Allow SSH access from anywhere (consider restricting to specific IPs in production)
  ingress {
    description = "SSH access"
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # Allow outbound traffic
  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${var.environment}-bastion-sg"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Security group rule to allow bastion host to access RDS
resource "aws_security_group_rule" "rds_bastion_ingress" {
  type                     = "ingress"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.bastion.id
  description              = "Allow PostgreSQL access from bastion host"
  security_group_id        = aws_security_group.rds.id
}

# Get the latest Amazon Linux 2023 AMI
data "aws_ami" "amazon_linux" {
  most_recent = true
  owners      = ["amazon"]

  filter {
    name   = "name"
    values = ["al2023-ami-*-x86_64"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

# IAM Role for Bastion Host
resource "aws_iam_role" "bastion" {
  name = "${var.environment}-bastion-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ec2.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Name        = "${var.environment}-bastion-role"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Attach AWS managed policy for SSM access
resource "aws_iam_role_policy_attachment" "bastion_ssm" {
  role       = aws_iam_role.bastion.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

# IAM Policy for RDS access (optional - for AWS CLI access to RDS)
resource "aws_iam_policy" "bastion_rds_access" {
  name        = "${var.environment}-bastion-rds-access"
  description = "Allow bastion host to access RDS via AWS CLI"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "rds:DescribeDBInstances",
          "rds:DescribeDBClusters",
          "rds:Connect"
        ]
        Resource = [
          "arn:aws:rds:${var.aws_region}:${data.aws_caller_identity.current.account_id}:db:${var.environment}-postgresql",
          "arn:aws:rds:${var.aws_region}:${data.aws_caller_identity.current.account_id}:cluster:*"
        ]
      }
    ]
  })

  tags = {
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Attach RDS access policy to bastion role
resource "aws_iam_role_policy_attachment" "bastion_rds_access" {
  role       = aws_iam_role.bastion.name
  policy_arn = aws_iam_policy.bastion_rds_access.arn
}

# Create instance profile for the bastion host
resource "aws_iam_instance_profile" "bastion" {
  name = "${var.environment}-bastion-profile"
  role = aws_iam_role.bastion.name

  tags = {
    Name        = "${var.environment}-bastion-profile"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# EC2 Instance for Bastion Host
resource "aws_instance" "bastion" {
  ami                    = data.aws_ami.amazon_linux.id
  instance_type          = "t3.micro"
  subnet_id              = aws_subnet.public.id
  vpc_security_group_ids = [aws_security_group.bastion.id]
  iam_instance_profile   = aws_iam_instance_profile.bastion.name
  
  # Use key pair if provided
  key_name = var.bastion_key_pair_name != "" ? var.bastion_key_pair_name : null

  # User data to install PostgreSQL client and ensure SSM agent is running
  user_data = <<-EOF
              #!/bin/bash
              sudo dnf update -y
              sudo dnf install -y postgresql15
              # Ensure SSM agent is running (should be pre-installed on AL2023)
              sudo systemctl enable amazon-ssm-agent
              sudo systemctl start amazon-ssm-agent
              EOF

  tags = {
    Name        = "${var.environment}-bastion"
    Environment = var.environment
    ManagedBy   = "tofu"
  }
}

# Outputs for Bastion Host
output "bastion_public_ip" {
  description = "Public IP address of the bastion host"
  value       = aws_instance.bastion.public_ip
}

output "bastion_instance_id" {
  description = "Instance ID of the bastion host"
  value       = aws_instance.bastion.id
}

output "bastion_ssh_command" {
  description = "SSH command to connect to the bastion host"
  value       = var.bastion_key_pair_name != "" ? "ssh -i your-key.pem ec2-user@${aws_instance.bastion.public_ip}" : "ssh ec2-user@${aws_instance.bastion.public_ip}"
}

output "bastion_ssm_command" {
  description = "AWS SSM Session Manager command to connect to the bastion host (no SSH key needed)"
  value       = "aws ssm start-session --target ${aws_instance.bastion.id} --region ${var.aws_region}"
}

output "bastion_rds_connection_command" {
  description = "Command to connect to RDS from bastion host"
  value       = "psql -h ${aws_db_instance.postgresql.address} -U ${var.db_username} -d ${var.db_name} -p ${var.db_port}"
}

output "bastion_iam_role_arn" {
  description = "IAM role ARN attached to the bastion host"
  value       = aws_iam_role.bastion.arn
}

