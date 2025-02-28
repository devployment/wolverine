// <auto-generated/>
#pragma warning disable
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using Wolverine.Http;

namespace Internal.Generated.WolverineHandlers
{
    // START: GET_read_name
    public class GET_read_name : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _options;

        public GET_read_name(Wolverine.Http.WolverineHttpOptions options) : base(options)
        {
            _options = options;
        }



        public override async System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var fakeEndpoint = new WolverineWebApi.FakeEndpoint();
            var name = (string)httpContext.GetRouteValue("name");
            var result_of_ReadStringArgument = fakeEndpoint.ReadStringArgument(name);
            await WriteString(httpContext, result_of_ReadStringArgument);
        }

    }

    // END: GET_read_name
    
    
}

