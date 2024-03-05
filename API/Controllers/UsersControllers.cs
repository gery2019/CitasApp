using System.Security.Claims;
using API.Controllers;
using API.DTO;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class UsersController : BaseApiController
{
    #region Private vars
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IPhotoService _photoService;
    public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _photoService = photoService;
    }
    #endregion

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
    {
        var users = await _userRepository.GetMembersAsync();
        return Ok(users);
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<MemberDto>> GetUser(string username)
    {
        return await _userRepository.GetMemberAsync(username);
    }
    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDtos memberUpdateDto)
    {
        var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
        
        if(user == null) return NotFound();

        _mapper.Map(memberUpdateDto, user);
        _userRepository.Update(user);

        if(await _userRepository.SaveAllAsync()) return NoContent();

        return BadRequest("No se pudo realizar la operación");
    }

    [HttpPost("photo")]
    public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
    {
        var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
        if(user == null) return NotFound();
        var result = await _photoService.AddPhotoAsync(file);
        if(result.Error != null) return BadRequest(result.Error.Message);
        var photo = new Photo
        {
            Url = result.SecureUrl.AbsoluteUri,
            PublicId = result.PublicId
        };
        if(user.Photos.Count == 0) photo.IsMain = true;
        user.Photos.Add(photo);
        if(await _userRepository.SaveAllAsync())
        {
            //return _mapper.Map<PhotoDto>(photo);
            return CreatedAtAction(nameof(GetUser),
            new {username = user.UserName},
            _mapper.Map<PhotoDto>(photo));
        }
        return BadRequest("No se pudo subir la foto");
    }
    [HttpPut("photo/{photoId}")]
    public async Task<ActionResult> SetMainPhoto(int photoId)
    {
        var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
        if(user == null) return NotFound("No se encontró el usuario");

        var newMain = user.Photos.FirstOrDefault(photo => photo.Id == photoId);
        if(newMain == null) return NotFound("No se encontró la foto");
        if(newMain.IsMain) return BadRequest("Esta ya es tu foto principal");

        var currentMain = user.Photos.FirstOrDefault(photo => photo.IsMain);
        if(currentMain != null) currentMain.IsMain = false;
        newMain.IsMain = true;

        if(await _userRepository.SaveAllAsync()) return NoContent();
        
        return BadRequest("No se pudo realizar la operación");
    }

    [HttpDelete("photo/{photoId}")]
    public async Task<ActionResult> DeletePhoto(int photoId)
    {
        var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
        var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);

        if(photo == null) return NotFound();
        if(photo.IsMain) return BadRequest("No puedes borrar tu foto principal");

        if(photo.PublicId != null)
        {
            var result = await _photoService.DeletePhotoAsync(photo.PublicId);
            if(result.Error != null) return BadRequest(result.Error.Message);
        }
        user.Photos.Remove(photo);
        
        if(await _userRepository.SaveAllAsync()) return Ok();
        return BadRequest("No se pudo borrar la foto");
    }
}