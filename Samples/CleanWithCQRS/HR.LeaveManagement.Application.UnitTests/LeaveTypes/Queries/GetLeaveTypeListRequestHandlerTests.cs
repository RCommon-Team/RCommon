using AutoMapper;
using HR.LeaveManagement.Application.DTOs.LeaveType;
using HR.LeaveManagement.Application.Features.LeaveTypes.Handlers.Queries;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Queries;
using HR.LeaveManagement.Application.Profiles;
using HR.LeaveManagement.Domain;
using Moq;
using NUnit.Framework;
using RCommon.Persistence;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Application.UnitTests.LeaveTypes.Queries
{
    [TestFixture()]
    public class GetLeaveTypeListRequestHandlerTests
    {
        private readonly IMapper _mapper;
        public GetLeaveTypeListRequestHandlerTests()
        {
            //_mockRepo = MockLeaveTypeRepository.GetLeaveTypeRepository();

            var mapperConfig = new MapperConfiguration(c => 
            {
                c.AddProfile<MappingProfile>();
            });

            _mapper = mapperConfig.CreateMapper();
        }

        [Test]
        public async Task GetLeaveTypeListTest()
        {
            var testData = new List<LeaveType>();
            for (int i = 0; i < 5; i++)
            {
                testData.Add(TestDataActions.CreateLeaveTypeStub());
            }
            var mock = new Mock<IGraphRepository<LeaveType>>();
            mock.Setup(x => x.FindAsync(x=>true, CancellationToken.None))
                .Returns(() => Task.FromResult(testData as ICollection<LeaveType>));
            
            var handler = new GetLeaveTypeListRequestHandler(mock.Object, _mapper);
            var result = await handler.Handle(new GetLeaveTypeListRequest(), CancellationToken.None);

            result.ShouldBeOfType<List<LeaveTypeDto>>();
            result.Count.ShouldBe(5);

        }
    }
}
