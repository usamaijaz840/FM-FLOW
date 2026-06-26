using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Api;
using EFRepository;
using FirebaseAdmin;
using FluentValidation;
using FMFlow.AccessValidation;
using FMFlow.Admin.Interface;
using FMFlow.Admin.Interface.DTOs;
using FMFlow.Admin.Service;
using FMFlow.Admin.Service.Validators;
using FMFlow.Common;
using FMFlow.Common.ReCaptcha;
using FMFlow.Common.Services;
using FMFlow.Customers.Interface;
using FMFlow.Customers.Interface.DTOs;
using FMFlow.Customers.Service;
using FMFlow.Customers.Service.Validators;
using FMFlow.Data;
using FMFlow.Data.Identity;
using FMFlow.Data.PostgresSeeder;
using FMFlow.Email.Service;
using FMFlow.Employees.Interface;
using FMFlow.Employees.Interface.DTOs;
using FMFlow.Employees.Service;
using FMFlow.Employees.Service.Validators;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.Estimates.Interface.DTOs.Examples;
using FMFlow.Estimates.Service;
using FMFlow.Estimates.Service.Validators;
using FMFlow.Events.Service;
using FMFlow.Files.Interface;
using FMFlow.Files.Interface.DTOs;
using FMFlow.Files.Service;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.Identity.Interface.DTOs;
using FMFlow.Identity.Service;
using FMFlow.Identity.Service.Validators;
using FMFlow.Integrations.Firebase;
using FMFlow.Integrations.MxMerchant.Interface;
using FMFlow.Integrations.Service;
using FMFlow.Leads.Interface;
using FMFlow.Leads.Interface.DTOs;
using FMFlow.Leads.Service;
using FMFlow.Leads.Service.Validators;
using FMFlow.LeadTimelines.Interface;
using FMFlow.LeadTimelines.Service;
using FMFlow.Login.Interface;
using FMFlow.Login.Interface.DTOs;
using FMFlow.Login.Service;
using FMFlow.Login.Service.Validators;
using FMFlow.Pro.Interface;
using FMFlow.Pro.Interface.Dtos;
using FMFlow.Pro.Service;
using FMFlow.Pro.Service.Validators;
using FMFlow.Projects.Interface;
using FMFlow.Projects.Interface.DTOs;
using FMFlow.Projects.Service;
using FMFlow.Projects.Service.Mapper;
using FMFlow.Projects.Service.Validators;
using FMFlow.ProPayments.Interface;
using FMFlow.ProPayments.Service;
using FMFlow.ProUser;
using FMFlow.ProUser.Interface;
using FMFlow.ProUser.Service;
using FMFlow.SMS.Interface;
using FMFlow.SMS.Service;
using FMFlow.Transactions.Interface;
using FMFlow.Transactions.Interface.DTOs;
using FMFlow.Transactions.Service;
using FMFlow.Transactions.Service.Validators;
using FMFlowAPI.Exceptions;
using FMFlowAPI.Filters;
using FMFlowAPI.Handlers;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using Refit;
using Swashbuckle.AspNetCore.Filters;
using FMFlowAPI.Configuration;
using FMFlow.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var configuration = builder.Configuration;
var host = builder.Host;

host.ConfigureLogger();

// Add services to the container.
builder.AddNpgsqlDataSource(connectionName: "FMFlowDB");
builder.Services.AddHealthChecks().AddNpgSql();

var connectionString = builder.Configuration.GetConnectionString("FMFlowDB");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var awsOptions = builder.Configuration.GetSection("AWS").Get<FileUploadSettings>();
var region = RegionEndpoint.GetBySystemName(awsOptions.Region);
var amazonS3Client = new AmazonS3Client(awsOptions.AccessKeyID, awsOptions.SecrectAccessKey, region);
builder.Services.AddSingleton<IAmazonS3>(amazonS3Client);
builder.Services.AddSingleton<ITransferUtility>(new TransferUtility(amazonS3Client));

builder.Services.AddHostedService<PostgresSQLSeederService>();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

// Bind rate limit configuration
var rateLimitOptions = new RateLimitOptions();
builder.Configuration.GetSection(RateLimitOptions.SectionName).Bind(rateLimitOptions);

// Add CORS
if (allowedOrigins != null)
{
	builder.Services.AddCors(options =>
	{
		options.AddPolicy("AllowConfiguredOrigins", policy =>
		{
			policy.WithOrigins(allowedOrigins)
				  .AllowAnyHeader()
				  .AllowAnyMethod();
		});
	});
}

builder.Services.AddRazorPages();

builder.Services.AddControllers(options =>
{
	options.Filters.Add<HttpResponseExceptionFilter>();
}).AddJsonOptions(options =>
{
	options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

builder.Services.AddSwaggerGen(c =>
{
	var securityScheme = new OpenApiSecurityScheme
	{
		Name = "JWT Authentication",
		Description = "Enter JWT Bearer token **_only_**",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		Reference = new OpenApiReference
		{
			Id = JwtBearerDefaults.AuthenticationScheme,
			Type = ReferenceType.SecurityScheme
		}
	};

	c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);

	c.AddSecurityRequirement(new OpenApiSecurityRequirement
		{
			{securityScheme, Array.Empty<string>()}
		});
	//
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "FM Flow API", Version = "v1" });

	var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
	var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
	c.IncludeXmlComments(xmlPath);

	c.SchemaFilter<JsonElementSchemaFilter>();

	c.ExampleFilters();
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<EstimateResponseDtoExample>();

RegisterMX(builder, configuration);

RegisterConfig(builder, configuration);

builder.Services.AddTransient<LoggingHandler>(sp =>
{
	var logger = sp.GetRequiredService<ILogger<ProPaymentsService>>();
	return new LoggingHandler(logger);
});

builder.Services.AddScoped<IAccessValidator, AccessValidator>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IApiUrlBuilder, ApiUrlBuilder>();

RegisterCustomServices(builder);
RegisterValidators(builder);

RegisterKeycloak(builder, configuration);

var smsEnabled = builder.Configuration["Twilio:SMSEnabled"];
var isSmsEnabled = smsEnabled != null && bool.TryParse(smsEnabled, out var enabled) && enabled;

if (isSmsEnabled)
{
	var twilioAccountSid = builder.Configuration["Twilio:AccountSid"];
	var twilioAuthToken = builder.Configuration["Twilio:AuthToken"];

	if (twilioAccountSid == null || twilioAuthToken == null)
		throw new InvalidOperationException("Twilio configuration is missing.");

	TwilioSMSService.InitializeTwilioClient(twilioAccountSid, twilioAuthToken, isSmsEnabled);
}
else
{
	// Initialize with SMS disabled
	TwilioSMSService.InitializeTwilioClient(string.Empty, string.Empty, false);
}

FirebaseApp.Create(new AppOptions()
{
	Credential = GoogleCredential.FromFile("./firebase-admin.json"),
	ProjectId = "fmflow-guru",
});

builder.Services
	.AddLocalization(options => options.ResourcesPath = "Resources")
	.AddRequestLocalization(options =>
	{
		options.SetDefaultCulture("en-US");
		options.AddSupportedCultures("en-US", "es-US");
		options.AddSupportedUICultures("en-US", "es-US");
	});

builder.Services.AddScoped<DbContext, ApplicationDbContext>();
builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddProjectServices();

// Built-in Rate Limiting policies
builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
	// Custom rejection handler with Retry-After header
	options.OnRejected = async (context, cancellationToken) =>
	{
		if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
		{
			context.HttpContext.Response.Headers.RetryAfter =
				((int)retryAfter.TotalSeconds).ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
		}

		context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
		await context.HttpContext.Response.WriteAsync(
			"Too many requests. Please try again later.",
			cancellationToken);
	};
	options.AddPolicy("InitialOnboardingToken", httpContext =>
		RateLimitPartition.GetFixedWindowLimiter(
			partitionKey: httpContext.User.FindFirst("jti")?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
			factory: _ => new FixedWindowRateLimiterOptions
			{
				AutoReplenishment = true,
				PermitLimit = rateLimitOptions.InitialOnboardingToken.PermitLimit,
				Window = TimeSpan.FromMinutes(rateLimitOptions.InitialOnboardingToken.WindowMinutes),
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit = 0
			}));

	options.AddPolicy("PlacesAutocomplete", httpContext =>
		RateLimitPartition.GetTokenBucketLimiter(
			partitionKey: httpContext.User.FindFirst("jti")?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
			factory: _ => new TokenBucketRateLimiterOptions
			{
				TokenLimit = rateLimitOptions.PlacesAutocomplete.TokenLimit,
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit = 0,
				ReplenishmentPeriod = TimeSpan.FromSeconds(rateLimitOptions.PlacesAutocomplete.ReplenishmentPeriodSeconds),
				TokensPerPeriod = rateLimitOptions.PlacesAutocomplete.TokensPerPeriod,
				AutoReplenishment = true
			}));

	options.AddPolicy("PlacesDetails", httpContext =>
		RateLimitPartition.GetFixedWindowLimiter(
			partitionKey: httpContext.User.FindFirst("jti")?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
			factory: _ => new FixedWindowRateLimiterOptions
			{
				AutoReplenishment = true,
				PermitLimit = rateLimitOptions.PlacesDetails.PermitLimit,
				Window = TimeSpan.FromMinutes(rateLimitOptions.PlacesDetails.WindowMinutes),
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit = 0
			}));
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();

	try
	{
		app.UseSwagger();

		app.UseSwaggerUI(options =>
		{
			options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
			options.RoutePrefix = string.Empty; // Swagger UI at the root
		});

		app.UseMigrationsEndPoint();
	}
	catch (Exception ex)
	{
		throw;
	}
}
else
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
	try
	{
		await next();
	}
	catch (Exception ex)
	{
		context.Response.StatusCode = StatusCodes.Status500InternalServerError;
		context.Response.ContentType = "application/json";

		var response = new
		{
			status = context.Response.StatusCode,
			statusText = ex.Message,
			error = ex.Message
		};

		await context.Response.WriteAsJsonAsync(response);
	}
});

app.UseRouting();

app.UseCors("AllowConfiguredOrigins");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseRequestLocalization(options => options.ApplyCurrentCultureToResponseHeaders = true);
//app.UseDataAnnotationsLocalization

app.MapControllers();
app.MapStaticAssets();
app.MapRazorPages()
	.WithStaticAssets();

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
dbContext.Database.Migrate(); // Applies migrations and creates DB if needed

app.Run();

static void RegisterCustomServices(WebApplicationBuilder builder)
{
	builder.Services.AddScoped<IAdminService, AdminService>();
	builder.Services.AddScoped<IEmployeeService, EmployeeService>();
	builder.Services.AddScoped<IIdentityService, IdentityService>();
	builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
	builder.Services.AddHttpContextAccessor();
	builder.Services.AddScoped<ICustomersService, CustomersService>();
	builder.Services.AddScoped<IFilesService, FilesService>();
	builder.Services.AddScoped<ILeadNotesService, LeadNotesService>();
	builder.Services.AddScoped<ILoginService, LoginService>();
	builder.Services.AddScoped<IValidator<ResetPasswordRequestDto>, ResetPasswordRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<MagicLinkDto>, MagicLinkDtoValidator>();
	builder.Services.AddScoped<IValidator<ResendEstimateReviewEmailRequestDto>, ResendEstimateReviewEmailRequestDtoValidator>();
	builder.Services.AddScoped<IPaymentService, ProPaymentsService>();
	builder.Services.AddScoped<ITransactionsService, TransactionsService>();
	builder.Services.AddScoped<IProUserService, ProUserService>();
	builder.Services.AddScoped<IPaintsService, PaintsService>();
	builder.Services.AddScoped<ISheensService, SheensService>();
	builder.Services.AddScoped<ILeadsService, LeadsService>();
	builder.Services.AddScoped<ILeadSourcesService, LeadSourcesService>();
	builder.Services.AddScoped<ILeadTimelineService, LeadTimelineService>();
	builder.Services.AddScoped<IProjectsService, ProjectsService>();
	builder.Services.AddScoped<IEstimatesService, EstimatesService>();
	builder.Services.AddScoped<IEstimateNotificationService, EstimateNotificationService>();
	builder.Services.AddScoped<IEstimateTypesService, EstimateTypesService>();
	builder.Services.AddScoped<IMagicLinkService, MagicLinkService>();
	builder.Services.AddScoped<IEstimateCalculatorService, EstimateCalculatorService>();
	builder.Services.AddScoped<IScheduledEstimatesService, ScheduledEstimatesService>();
	builder.Services.AddScoped<IEstimateFilesService, EstimateFilesService>();
	builder.Services.AddScoped<IEstimateRecipientsService, EstimateRecipientsService>();
	builder.Services.AddScoped<IPaintSheenPricesService, PaintSheenPricesService>();
	builder.Services.AddScoped<IProWeekDayAvailabilitiesService, ProWeekDayAvailabilitiesService>();
	builder.Services.AddScoped<IProUserFileService, ProUserFileService>();
	builder.Services.AddScoped<IEstimateNotesService, EstimateNotesService>();
	builder.Services.AddScoped<IJobsService, JobsService>();
	builder.Services.AddScoped<IJobNotesService, JobNotesService>();
	builder.Services.AddScoped<IColorsService, ColorsService>();
	builder.Services.AddScoped<IMxService, MxService>();
	builder.Services.AddScoped<ProjectMapper>();
	builder.Services.AddScoped<IPaintsIngestor, PaintsIngestor>();
	builder.Services.AddScoped<ICustomerTempProsService, CustomerTempProsService>();
	builder.Services.AddScoped<IEventsService, EventsService>();
	builder.Services.AddScoped<INonceService, NonceService>();
	builder.Services.AddScoped<IFCMService, FCMService>();

	builder.Services.AddScoped<IJobCompletionService, JobCompletionService>();

	// Register Email Services
	builder.Services.AddEmailServices(builder.Configuration);

	// Register SMS Services
	builder.Services.AddScoped<ISMSSenderService, SMSSenderService>();

	// Register Integration Services
	builder.Services.AddIntegrationServices(builder.Configuration);

	// Register reCAPTCHA Service
	builder.Services.AddScoped<IReCaptchaService, ReCaptchaService>();

	// Register seed status Service
	builder.Services.AddSingleton<ISeedStatusService, SeedStatusService>();
}

static void RegisterValidators(WebApplicationBuilder builder)
{
	builder.Services.AddScoped<IValidator<CustomerLeadRequestDto>, CustomerLeadRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<EmployeeRequestDto>, EmployeeDtoValidator>();
	builder.Services.AddScoped<IValidator<EstimateNoteRequestDto>, EstimateNoteRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<EstimateRequestDto>, EstimateRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<LeadRequestDto>, LeadRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<LeadUpdateRequestDto>, LeadUpdateRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<LeadNoteRequestDto>, LeadNoteRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<LeadSourceRequestDto>, LeadSourceRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<CustomerRequestDto>, CustomerRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<NonceRequestDto>, NonceDtoValidator>();
	builder.Services.AddScoped<IValidator<PaintSheenPriceRequestDto>, PaintSheenPriceRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<PaintRequestDto>, PaintRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<ProjectRequestDto>, ProjectRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<CustomerProjectRequestDto>, CustomerProjectRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<ProjectUpdateRequestDto>, ProjectUpdateRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<ProUserDto>, ProUserDTOValidator>();
	builder.Services.AddScoped<IValidator<RefreshTokenRequestDto>, RefreshTokenRequestDTOValidator>();
	builder.Services.AddScoped<IValidator<RequestedEstimateRequestDto>, RequestedEstimateRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<ScheduledEstimateRequestDto>, ScheduledEstimateRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<SavePasswordRequestDto>, SavePasswordRequestDTOValidator>();
	builder.Services.AddScoped<IValidator<SavePasswordRequestDto>, SavePasswordRequestDTOValidator>();
	builder.Services.AddScoped<IValidator<SheenRequestDto>, SheenRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<PaymentRequestDto>, PaymentRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<JobRequestDto>, JobRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<JobNoteRequestDto>, JobNoteRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<EstimateSendEmailsRequestDto>, EstimateSendEmailsRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<DiscountRequestDto>, DiscountRequestDtoValidator>();
	builder.Services.AddScoped<IValidator<ServiceAccountClientTokenRequestDto>, ServiceAccountClientTokenRequestDtoValidator>();
}

static void RegisterKeycloak(WebApplicationBuilder builder, ConfigurationManager configuration)
{
	var keycloakAuthUrl =
		   builder.Configuration["Keycloak:InternalUrl"] ??
		   builder.Configuration["Keycloak:auth-server-url"] ??
		   throw new InvalidOperationException("Keycloak URL not found.");

	var keycloakRealm = builder.Configuration["Keycloak:realm"]
		?? throw new InvalidOperationException("Keycloak realm not found in configuration.");

	var keycloakClientId = builder.Configuration["Keycloak:resource"]
		?? throw new InvalidOperationException("Keycloak resource not found in configuration.");

	var keycloakSecret = builder.Configuration["Keycloak:credentials:secret"]
		?? throw new InvalidOperationException("Keycloak secret not found in configuration.");

	var keycloakRequireHttpsMetadata = bool.Parse(builder.Configuration["Keycloak:RequireHttpsMetadata"]
		?? throw new InvalidOperationException("Keycloak RequireHttpsMetadata not found in configuration."));

	builder.Services.AddHttpClient("keycloak", client =>
	{
		client.BaseAddress = new Uri(keycloakAuthUrl.TrimEnd('/') + "/");
	});

	builder.Services.Configure<KeycloakConfiguration>(opt =>
	{
		opt.KeycloakBaseUrl = keycloakAuthUrl; // normalized source of truth
		opt.KeycloakRealm = keycloakRealm;
		opt.ClientId = keycloakClientId;
		opt.ClientSecret = keycloakSecret;
	});

	builder.Services.AddScoped<IIdentityRepository>(sp =>
		new KeycloakRepository(
			sp.GetRequiredService<IHttpClientFactory>(),
			sp.GetRequiredService<IOptions<KeycloakConfiguration>>(),
			sp.GetRequiredService<ILogger<KeycloakRepository>>()
		)
	);

	// Configure CustomJwtConfiguration using options pattern
	builder.Services.Configure<CustomJwtConfiguration>(builder.Configuration.GetSection("CustomJwt"));

	builder.Services.AddScoped<ICustomJwtService>(sp => new CustomJwtService(
		sp.GetRequiredService<IOptions<CustomJwtConfiguration>>(),
		new JwtSecurityTokenHandler()
	));

	builder.Services.AddScoped<IOnboardingTokenService, OnboardingTokenService>();

	// Register Keycloak JWT scheme
	builder.Services.AddAuthentication(nameof(AuthScheme.Keycloak))
		.AddJwtBearer(nameof(AuthScheme.Keycloak), options =>
		{
			var isTest = Environment.GetEnvironmentVariable("ASPIRE_TEST") == "true";
			if (!isTest)
			{
				// ---- NORMAL (non-test) CONFIG ----
				options.Authority = $"{keycloakAuthUrl.TrimEnd('/')}/realms/{keycloakRealm}";
				options.Audience = keycloakClientId;
				options.RequireHttpsMetadata = keycloakRequireHttpsMetadata;

				options.TokenValidationParameters = new TokenValidationParameters
				{
					RoleClaimType = ClaimTypes.Role
				};
			}
			else
			{
				options.Authority = null;
				options.MetadataAddress = null;
				options.RequireHttpsMetadata = false;

				// Use new handler pipeline (do NOT enable UseSecurityTokenValidators here)
				options.TokenHandlers.Clear();
				options.TokenHandlers.Add(new TestPermissiveJwtHandler());

				options.TokenValidationParameters = new TokenValidationParameters
				{
					RoleClaimType = ClaimTypes.Role,
					ValidateIssuer = false,
					ValidateAudience = false,
					ValidateIssuerSigningKey = false,
					ValidateLifetime = false,
					RequireSignedTokens = false
				};
			}

			options.Events = new JwtBearerEvents
			{
				OnTokenValidated = context =>
				{
					var user = context.Principal;

					var identity = (ClaimsIdentity?)user?.Identity;
					var realmRoles = context.Principal?.FindFirst("realm_access")?.Value;

					if (identity != null && !string.IsNullOrEmpty(realmRoles))
					{
						var realmRolesObj = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(realmRoles);

						if (realmRolesObj != null && realmRolesObj.TryGetValue("roles", out var roles))
						{
							foreach (var role in roles)
							{
								var roleToAssign = role;

								if (Enum.TryParse(typeof(Roles), role, true, out var parsedRole))
									roleToAssign = parsedRole.ToString();

								if (!string.IsNullOrEmpty(roleToAssign))
									identity.AddClaim(new Claim(ClaimTypes.Role, roleToAssign));
							}
						}
					}

					return Task.CompletedTask;
				}
			};
		})
		// Register custom JWT scheme for use with magic links
		.AddJwtBearer(nameof(AuthScheme.CustomJwt), options => {
			var customJwtConfig = builder.Configuration.GetSection("CustomJwt").Get<CustomJwtConfiguration>()!;
			options.TokenValidationParameters = CustomJwtService.CreateValidationParameters(customJwtConfig);
			
			// Additional security settings
			options.SaveToken = false; // Don't save tokens in AuthenticationProperties
			options.IncludeErrorDetails = !builder.Environment.IsProduction(); // Hide error details in production
		});

	// Default policy for Keycloak
	builder.Services.AddAuthorizationBuilder()
		.AddPolicy(Policies.CommonRoles, policy =>
		policy.RequireRole(
			nameof(Roles.SuperAdmin),
			nameof(Roles.AccountManager),
			nameof(Roles.Pro),
			nameof(Roles.Customer),
			nameof(Roles.TempCustomer),
			nameof(Roles.Scheduler),
			nameof(Roles.ChatBot))
		)
		.AddPolicy("CustomerOnboarding", policy =>
			policy
				.RequireRole(nameof(Roles.Customer))
				.RequireClaim(CustomClaimTypes.TokenPurpose, "onboarding")
		)
		.AddPolicy(Policies.SA_AM_PRO_SCH, policy =>
		policy.RequireRole(
			nameof(Roles.SuperAdmin),
			nameof(Roles.AccountManager),
			nameof(Roles.Scheduler),
			nameof(Roles.Pro))
		)
		.AddPolicy(Policies.FM_Employees, policy =>
		policy.RequireRole(
			nameof(Roles.SuperAdmin),
			nameof(Roles.AccountManager),
			nameof(Roles.Scheduler))
		);
}

static void RegisterMX(WebApplicationBuilder builder, ConfigurationManager configuration)
{
	// Add MXConnect API configuration
	builder.Services.AddHttpClient<IMxMerchantApi>(client =>
	{
		var baseUrl = configuration["MXConnect:BaseUrl"]
			?? throw new InvalidOperationException("MXConnect:BaseUrl not found in configuration.");
		client.BaseAddress = new Uri(baseUrl);
	})
	.AddHttpMessageHandler<LoggingHandler>();

	// Configure Refit for MXMerchant API
	builder.Services.AddTransient(sp =>
	{
		var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
		var client = httpClientFactory.CreateClient(nameof(IMxMerchantApi));
		return RestService.For<IMxMerchantApi>(client);
	});
}

static void RegisterConfig(WebApplicationBuilder builder, ConfigurationManager configuration)
{
	builder.Services.Configure<AppSettings>(configuration.GetSection("App"));
	builder.Services.Configure<GoogleSettings>(configuration.GetSection("Google"));
	builder.Services.Configure<OutlookSettings>(configuration.GetSection("Outlook"));
	builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection("AWS"));
	builder.Services.Configure<NonceSettings>(configuration.GetSection("NonceSettings"));

	builder.Services.AddSingleton(sp =>
		sp.GetRequiredService<IOptions<AppSettings>>().Value);
	builder.Services.AddSingleton(sp =>
		sp.GetRequiredService<IOptions<GoogleSettings>>().Value);
	builder.Services.AddSingleton(sp =>
		sp.GetRequiredService<IOptions<OutlookSettings>>().Value);
	builder.Services.AddSingleton(sp =>
		sp.GetRequiredService<IOptions<FileUploadSettings>>().Value);
	builder.Services.AddSingleton(sp =>
		sp.GetRequiredService<IOptions<NonceSettings>>().Value);
}


sealed class TestPermissiveJwtHandler : JsonWebTokenHandler
{
	public override Task<TokenValidationResult> ValidateTokenAsync(
		string token, TokenValidationParameters validationParameters)
	{
		// Parse without validating
		var jwt = ReadJsonWebToken(token);

		var identity = new ClaimsIdentity(
			jwt.Claims,
			JwtBearerDefaults.AuthenticationScheme,
			ClaimTypes.Name,
			ClaimTypes.Role);

		var result = new TokenValidationResult
		{
			IsValid = true,
			ClaimsIdentity = identity,
			SecurityToken = jwt
		};

		return Task.FromResult(result);
	}
}
