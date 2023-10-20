﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using TicketNow.Domain.Dtos.Default;
using TicketNow.Domain.Dtos.User;
using TicketNow.Domain.Extensions;
using TicketNow.Domain.Filters;
using TicketNow.Domain.Interfaces.Services;
using TicketNow.Domain.Utilities;

namespace TicketNow.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = StaticUserRoles.ADMIN)]
        [SwaggerOperation(Summary = "Get all users")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(IList<UserResponseDto>))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        public IActionResult GetAll([FromQuery] UserFilter filter)
        {
            var users = _userService.GetAll(filter);
            if (users is null)
                return NotFound();

            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = StaticUserRoles.ADMIN)]
        [SwaggerOperation(Summary = "Get user by id")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(UserResponseDto))]
        [SwaggerResponse((int)HttpStatusCode.NoContent)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, Type = typeof(IReadOnlyCollection<dynamic>))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        public IActionResult GetById(int id)
        {
            var user = _userService.GetById(id);
            if (user is null)
                return NotFound();

            return Ok(user);
        }

        [HttpPut]
        [Authorize(Roles = StaticUserRoles.CUSTOMER)]
        [SwaggerOperation(Summary = "Update user")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(DefaultServiceResponseDto))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, Type = typeof(IReadOnlyCollection<dynamic>))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Update([FromBody] UpdateUserDto updateUserDto)
        {
            var response = await _userService.Update(updateUserDto, this.GetUserIdLogged());
            return Ok(response);
        }

        [HttpPut("Password")]
        [Authorize]
        [SwaggerOperation(Summary = "Change password")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(DefaultServiceResponseDto))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, Type = typeof(IReadOnlyCollection<dynamic>))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdateUserPasswordDto updateUserPasswordDto)
        {
            var response = await _userService.UpdatePassword(updateUserPasswordDto, this.GetUserIdLogged());
            return Ok(response);
        }

        [HttpPost("UploadPhoto")]
        [Authorize(Roles = StaticUserRoles.CUSTOMER)]
        [SwaggerOperation(Summary = "Upload Photo")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(DefaultServiceResponseDto))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, Type = typeof(IReadOnlyCollection<dynamic>))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            var response = await _userService.UploadPhoto(file, this.GetUserIdLogged());
            return Ok(response);
        }
    }
}
