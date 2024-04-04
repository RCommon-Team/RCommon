using HR.LeaveManagement.Api.Middleware;
using HR.LeaveManagement.Application;
using HR.LeaveManagement.Identity;
using HR.LeaveManagement.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using RCommon;
using RCommon.Emailing.SendGrid;
using RCommon.Persistence.EFCore;
using RCommon.Security;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using RCommon.Persistence.Transactions;
using RCommon.Mediator.MediatR;
using System.Reflection;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Requests.Commands;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Handlers.Commands;
using HR.LeaveManagement.Application.Responses;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Requests.Queries;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Handlers.Queries;
using HR.LeaveManagement.Application.DTOs.LeaveAllocation;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Commands;
using HR.LeaveManagement.Application.Features.LeaveRequests.Handlers.Commands;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Commands;
using HR.LeaveManagement.Application.Features.LeaveTypes.Handlers.Commands;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Queries;
using HR.LeaveManagement.Application.Features.LeaveRequests.Handlers.Queries;
using HR.LeaveManagement.Application.DTOs.LeaveRequest;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Queries;
using HR.LeaveManagement.Application.Features.LeaveTypes.Handlers.Queries;
using HR.LeaveManagement.Application.DTOs.LeaveType;

var builder = WebApplication.CreateBuilder(args);


AddSwaggerDoc(builder.Services);
builder.Services.AddControllers();

// Add RCommon services
builder.Services.AddRCommon()
    .WithClaimsAndPrincipalAccessor()
    .WithSendGridEmailServices(x =>
    {
        var sendGridSettings = builder.Configuration.Get<SendGridEmailSettings>();
        x.SendGridApiKey = sendGridSettings.SendGridApiKey;
        x.FromNameDefault = sendGridSettings.FromNameDefault;
        x.FromEmailDefault = sendGridSettings.FromEmailDefault;
    })
    .WithDateTimeSystem(dateTime => dateTime.Kind = DateTimeKind.Utc)
    .WithSequentialGuidGenerator(guid => guid.DefaultSequentialGuidType = SequentialGuidType.SequentialAsString)
    .WithMediator<MediatRBuilder>(mediator =>
    {
        mediator.AddRequest<CreateLeaveAllocationCommand, BaseCommandResponse, CreateLeaveAllocationCommandHandler>();
        mediator.AddRequest<DeleteLeaveAllocationCommand, DeleteLeaveAllocationCommandHandler>();
        mediator.AddRequest<UpdateLeaveAllocationCommand, UpdateLeaveAllocationCommandHandler>();
        mediator.AddRequest<GetLeaveAllocationDetailRequest, LeaveAllocationDto, GetLeaveAllocationDetailRequestHandler>();
        mediator.AddRequest<GetLeaveAllocationListRequest, List<LeaveAllocationDto>, GetLeaveAllocationListRequestHandler>();
        mediator.AddRequest<CreateLeaveAllocationCommand, BaseCommandResponse, CreateLeaveAllocationCommandHandler>();
        mediator.AddRequest<DeleteLeaveAllocationCommand,  DeleteLeaveAllocationCommandHandler>();
        mediator.AddRequest<UpdateLeaveAllocationCommand, UpdateLeaveAllocationCommandHandler>();
        mediator.AddRequest<CreateLeaveRequestCommand, BaseCommandResponse, CreateLeaveRequestCommandHandler>();
        mediator.AddRequest<DeleteLeaveRequestCommand, DeleteLeaveRequestCommandHandler>();
        mediator.AddRequest<UpdateLeaveRequestCommand, UpdateLeaveRequestCommandHandler>();
        mediator.AddRequest<CreateLeaveTypeCommand, BaseCommandResponse, CreateLeaveTypeCommandHandler>();
        mediator.AddRequest<GetLeaveRequestDetailRequest, LeaveRequestDto, GetLeaveRequestDetailRequestHandler>();
        mediator.AddRequest<GetLeaveRequestListRequest, List<LeaveRequestListDto>, GetLeaveRequestListRequestHandler>();
        mediator.AddRequest<CreateLeaveTypeCommand, BaseCommandResponse, CreateLeaveTypeCommandHandler>();
        mediator.AddRequest<DeleteLeaveTypeCommand, DeleteLeaveTypeCommandHandler>();
        mediator.AddRequest<UpdateLeaveTypeCommand, UpdateLeaveTypeCommandHandler>();
        mediator.AddRequest<GetLeaveTypeDetailRequest, LeaveTypeDto, GetLeaveTypeDetailRequestHandler>();
        mediator.AddRequest<GetLeaveTypeListRequest, List<LeaveTypeDto>, GetLeaveTypeListRequestHandler>();

        mediator.Configure(config =>
        {
            config.RegisterServicesFromAssemblies((typeof(ApplicationServicesRegistration).GetTypeInfo().Assembly));
        });
        mediator.AddLoggingToRequestPipeline();
        mediator.AddUnitOfWorkToRequestPipeline();
    })
    .WithPersistence<EFCorePerisistenceBuilder, DefaultUnitOfWorkBuilder>(ef => // Repository/ORM configuration. We could easily swap out to NHibernate without impact to domain service up through the stack
    {
        // Add all the DbContexts here
        ef.AddDbContext<LeaveManagementDbContext>(DataStoreNamesConst.LeaveManagement, options =>
        {
            options.UseSqlServer(
                builder.Configuration.GetConnectionString(DataStoreNamesConst.LeaveManagement));
        });
        ef.SetDefaultDataStore(dataStore =>
        {
            dataStore.DefaultDataStoreName = DataStoreNamesConst.LeaveManagement;
        });
    }, unitOfWork =>
    {
        unitOfWork.SetOptions(options =>
        {
            options.AutoCompleteScope = true;
            options.DefaultIsolation = IsolationLevel.ReadCommitted;
        });
    });

// Add services to the container.
builder.Services.ConfigureApplicationServices();
builder.Services.ConfigureIdentityServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o =>
{
    o.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

Console.WriteLine(builder.Services.GenerateServiceDescriptorsString());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HR.LeaveManagement.Api v1"));
app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("CorsPolicy");

app.MapControllers();

app.Run();

void AddSwaggerDoc(IServiceCollection services)
{
    services.AddSwaggerGen(c =>
    {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = @"JWT Authorization header using the Bearer scheme. 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                  {
                    {
                      new OpenApiSecurityScheme
                      {
                        Reference = new OpenApiReference
                          {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                          },
                          Scheme = "oauth2",
                          Name = "Bearer",
                          In = ParameterLocation.Header,

                        },
                        new List<string>()
                      }
                    });

        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "HR Leave Management Api",

        });

    });
}
