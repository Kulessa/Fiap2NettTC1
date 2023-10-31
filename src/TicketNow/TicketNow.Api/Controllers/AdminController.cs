﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using TicketNow.Domain.Dtos.Default;
using TicketNow.Domain.Dtos.User;
using TicketNow.Domain.Interfaces.Services;
using TicketNow.Domain.Utilities;

namespace TicketNow.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;

        public AdminController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPut]
        [Route("activate")]
        [Authorize(Roles = StaticUserRoles.ADMIN)]
        [SwaggerOperation(Summary = "Activate and deactivate user")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(DefaultServiceResponseDto))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, Type = typeof(IReadOnlyCollection<dynamic>))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Activate([FromBody] ActivateUserDto activateUserDto)
        {
            var response = await _userService.ActivateAsync(activateUserDto);
            return Ok(response);
        }

        [HttpPut]
        [Route("approve/{id}")]
        [Authorize(Roles = StaticUserRoles.ADMIN)]
        [SwaggerOperation(Summary = "Approve promoter user")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(DefaultServiceResponseDto))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, Type = typeof(IReadOnlyCollection<dynamic>))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Approve([FromQuery] int id)
        {
            var response = await _userService.ApproveAsync(id);
            return Ok(response);
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = StaticUserRoles.ADMIN)]
        [SwaggerOperation(Summary = "Delete user")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(DefaultServiceResponseDto))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _userService.Delete(id);
            return Ok(response);
        }
    }
}
