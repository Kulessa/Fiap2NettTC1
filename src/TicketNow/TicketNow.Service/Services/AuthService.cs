﻿using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TicketNow.Domain.Dtos.Auth;
using TicketNow.Domain.Dtos.Default;
using TicketNow.Domain.Entities;
using TicketNow.Domain.Interfaces.Services;
using TicketNow.Domain.Utilities;
using TicketNow.Service.Validators.Auth;

namespace TicketNow.Service.Services
{
    public class AuthService : BaseService, IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public AuthService(UserManager<User> userManager,
            IConfiguration configuration,
            IMapper mapper)
        {
            _userManager = userManager;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            var validationResult = Validate(loginDto, Activator.CreateInstance<LoginValidator>());
            if (!validationResult.IsValid) { throw new ValidationException(validationResult.Errors); } //todo: add notification

            var user = await _userManager.FindByNameAsync(loginDto.Username);
            if (user is null || !user.Active) { throw new ValidationException("Credenciais invalidas!"); } //todo: add notification

            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordCorrect) { throw new ValidationException("Credenciais invalidas!"); } //todo: add notification

            var authClaims = await GetAuthClaims(user);
            var tokenObject = GenerateNewJsonWebToken(authClaims);
            var refreshToken = GenerateRefreshToken();

            _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInHours"],
                   out int refreshTokenValidityInHours);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddHours(refreshTokenValidityInHours);
            await _userManager.UpdateAsync(user);

            return new LoginResponseDto
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(tokenObject),
                RefreshToken = refreshToken,
                Expires = tokenObject.ValidTo,
            };
        }

        public async Task<DefaultServiceResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var validationResult = Validate(registerDto, Activator.CreateInstance<RegisterValidator>());
            if (!validationResult.IsValid) { throw new ValidationException(validationResult.Errors); } //todo: add notification

            var existsUser = await _userManager.FindByNameAsync(registerDto.Username);
            if (existsUser is not null) { throw new ValidationException("Usuario já cadastrado"); } //todo: add notification

            var newUser = _mapper.Map<User>(registerDto);

            newUser.CreatedAt = DateTime.Now;
            newUser.Active = true;

            var createUserResult = await _userManager.CreateAsync(newUser, registerDto.Password);

            if (!createUserResult.Succeeded)
                throw new Exception(string.Join(" ", createUserResult.Errors.Select(t => t.Code + " - " + t.Description)));  //todo: add notification                          

            await _userManager.AddToRoleAsync(newUser, StaticUserRoles.CUSTOMER);

            return new DefaultServiceResponseDto
            {
                Success = true,
                Message = "Usuario criado com sucesso!" //todo: melhorar isso
            };
        }

        public async Task<DefaultServiceResponseDto> RevokeAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) { throw new ValidationException("Usuario não encontrado!"); } //todo: add notification

            user.RefreshToken = null;
            await _userManager.UpdateAsync(user);

            return new DefaultServiceResponseDto
            {
                Success = true,
                Message = "Token revogado com sucesso!" //todo: melhorar isso
            };
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(string accessToken, string refreshToken, string userName)
        {
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrEmpty(refreshToken))
                throw new ValidationException("Token invalido!"); //add notification            
            
            var user = await _userManager.FindByNameAsync(userName);

            if (user is null ||
                user.RefreshToken != refreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.Now)
                throw new ValidationException("Token invalido!"); //add notification            

            var authClaims = await GetAuthClaims(user);
            var tokenObject = GenerateNewJsonWebToken(authClaims);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _userManager.UpdateAsync(user);

            return new LoginResponseDto
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(tokenObject),
                RefreshToken = newRefreshToken,
                Expires = tokenObject.ValidTo
            };
        }

        private JwtSecurityToken GenerateNewJsonWebToken(List<Claim> claims)
        {
            var authSecret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            _ = int.TryParse(_configuration["JWT:TokenValidityInHours"],
                out int tokenValidityInHours);

            return new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(tokenValidityInHours),
                claims: claims,
                signingCredentials: new SigningCredentials(authSecret, SecurityAlgorithms.HmacSha256)
                );
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        } 

        private async Task<List<Claim>> GetAuthClaims(User user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("JWTID", Guid.NewGuid().ToString()),
            };

            foreach (var userRole in userRoles)
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));

            return authClaims;
        }
    }
}