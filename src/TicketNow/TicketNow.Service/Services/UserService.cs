﻿using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using TicketNow.Domain.Dtos.Default;
using TicketNow.Domain.Dtos.User;
using TicketNow.Domain.Entities;
using TicketNow.Domain.Extensions;
using TicketNow.Domain.Filters;
using TicketNow.Domain.Interfaces.Repositories;
using TicketNow.Domain.Interfaces.Services;
using TicketNow.Infra.CrossCutting.Azure;
using TicketNow.Infra.CrossCutting.Notifications;
using TicketNow.Service.Validators.User;

namespace TicketNow.Service.Services
{
    public class UserService : BaseService, IUserService
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly NotificationContext _notificationContext;

        public UserService(
            IConfiguration configuration,
            IUserRepository userRepository,
            UserManager<User> userManager,
            IMapper mapper,
            NotificationContext notificationContext)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _userManager = userManager;
            _mapper = mapper;
            _notificationContext = notificationContext;
        }

        public ICollection<UserResponseDto> GetAll(UserFilter filter)
        {
            var users = _userRepository
                .Select()
                .AsQueryable()
                .OrderByDescending(u => u.CreatedAt)
                .ApplyFilter(filter);

            if (filter.FirstName is not null)
                users = users.Where(u => u.FirstName == filter.FirstName);

            if (filter.LastName is not null)
                users = users.Where(u => u.LastName == filter.LastName);

            if (filter.Document is not null)
                users = users.Where(u => u.Document == filter.Document);

            if (filter.Active is not null)
                users = users.Where(u => u.Active == filter.Active);

            var response = _mapper.Map<List<UserResponseDto>>(users);

            return response;
        }

        public UserResponseDto GetById(int id)
        {
            var user = _userRepository.Select(id);
            var response = _mapper.Map<UserResponseDto>(user);
            return response;
        }

        public async Task<DefaultServiceResponseDto> Update(UpdateUserDto updateUserDto, int id)
        {
            var validationResult = Validate(updateUserDto, Activator.CreateInstance<UpdateUserValidator>());
            if (!validationResult.IsValid) { _notificationContext.AddNotifications(validationResult.Errors); return default; }

            var existsUser = await _userManager.FindByNameAsync(updateUserDto.Username);
            if (existsUser is not null && existsUser.Id != id) { _notificationContext.AddNotification(StaticNotifications.UsernameAlreadyExists); return default; }

            var user = await _userManager.FindByIdAsync(id.ToString());
            _mapper.Map(updateUserDto, user);
            var updateUserResult = await _userManager.UpdateAsync(user);

            if (!updateUserResult.Succeeded)
            {
                var errors = updateUserResult.Errors.Select(t => new Notification(t.Code, t.Description));
                _notificationContext.AddNotifications(errors);
                return default;
            }

            return new DefaultServiceResponseDto
            {
                Success = true,
                Message = StaticNotifications.UserEdited.Message
            };
        }

        public async Task<DefaultServiceResponseDto> UpdatePassword(UpdateUserPasswordDto updateUserPasswordDto, int id)
        {
            var validationResult = Validate(updateUserPasswordDto, Activator.CreateInstance<UpdateUserPasswordValidator>());
            if (!validationResult.IsValid) { _notificationContext.AddNotifications(validationResult.Errors); return default; }

            var user = await _userManager.FindByIdAsync(id.ToString());
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, updateUserPasswordDto.CurrentPassword, updateUserPasswordDto.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                var errors = changePasswordResult.Errors.Select(t => new Notification(t.Code, t.Description));
                _notificationContext.AddNotifications(errors);
                return default;
            }

            return new DefaultServiceResponseDto
            {
                Success = true,
                Message = StaticNotifications.PasswordChanged.Message
            };
        }

        public async Task<DefaultServiceResponseDto> UploadPhoto(IFormFile file, int id)
        {
            var validationResult = Validate(file, Activator.CreateInstance<PhotoValidator>());
            if (!validationResult.IsValid) { _notificationContext.AddNotifications(validationResult.Errors); return default; }

            var connectionString = _configuration["Azure:BlobStorage:ConnectionString"];
            var blobClient = new AzureBlobClient(connectionString);

            var containerName = _configuration["Azure:BlobStorage:ContainerName"];
            var stream = file.OpenReadStream();
            await blobClient.Upload(stream, id.ToString(), containerName);

            return new DefaultServiceResponseDto
            {
                Success = true,
                Message = StaticNotifications.PasswordChanged.Message
            };
        }
    }
}
