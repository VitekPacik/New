using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aton_7.Model;
using Azure.Core;

namespace Aton_7.Controllers
{
    [Route("api/User")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private static List<User> _users = new List<User>();
        private const string AdminLogin = "admin";
        private const string AdminPassword = "admin";
        private bool CheckCredentials(string login, string password)
        {
            // Проверка на соответствие заранее заданным значениям логина и пароля
            if (login == AdminLogin && password == AdminPassword)
            {
                return true; // Пользователь является администратором
            }

            // Дополнительные проверки для других пользователей (если необходимо)
            var user = _users.FirstOrDefault(u => u.Login == login && u.Password == password);
            if (user != null && user.Admin)
            {
                return true; // Пользователь является администратором
            }

            return false; // Пользователь не является администратором
        }

        // Модель запроса для обновления имени, пола и даты рождения пользователя
        private bool ValidateLogin(string login)
        {
            // Проверка, что логин состоит только из латинских букв и цифр
            return System.Text.RegularExpressions.Regex.IsMatch(login, "^[a-zA-Z0-9]+$");
        }

        private bool ValidatePassword(string password)
        {
            // Проверка, что пароль состоит только из латинских букв и цифр
            return System.Text.RegularExpressions.Regex.IsMatch(password, "^[a-zA-Z0-9]+$");
        }

        private bool ValidateName(string name)
        {
            // Проверка, что имя состоит только из латинских и русских букв
            return System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-zA-Zа-яА-Я]+$");
        }

        public class UserUpdateRequest
        {
            public string OldPassword { get; set; }
            public string Name { get; set; }
            public int Gender { get; set; }
            public DateTime? Birthday { get; set; }
        }

        // Модель запроса для обновления пароля пользователя
        public class PasswordUpdateRequest
        {
            public string OldPassword { get; set; }
            public string NewPassword { get; set; }
        }

        // Модель запроса для обновления логина пользователя
        public class LoginUpdateRequest
        {
            public string OldPassword { get; set; }
            public string NewLogin { get; set; }
        }

        private bool IsValidGender(int gender)
        {
            return gender >= 0 && gender <= 2;
        }

        /*private bool IsAdmin(string login)
        {
            var user = _users.FirstOrDefault(u => u.Login == login);
            return user != null && user.Admin;
        }*/



        [HttpPost("create(1)")]
        public IActionResult Create(string login, string password, string name, int gender, DateTime? birthday, bool admin, string adminLogin, string adminPassword)
        {

            if (CheckCredentials(adminLogin, adminPassword))
            {
                // Проверка наличия пользователя с таким логином
                if (_users.Any(u => u.Login == login))
                {
                    return BadRequest("Пользователь с таким логином уже существует.");
                }
                
                // Создание нового пользователя
                var newUser = new User
                {
                    Guid = Guid.NewGuid(),
                    Login = login,
                    Password = password,
                    Name = name,
                    Gender = gender,
                    Birthday = birthday,
                    Admin = admin,
                    CreatedOn = DateTime.Now,
                    CreatedBy = AdminLogin,
                    ModifiedOn = DateTime.Now,
                    ModifiedBy = AdminLogin,
                    RevokedOn = default,
                    RevokedBy = default
                };
                if (!ValidateLogin(login))
                {
                    return BadRequest("Неверный формат логина.");
                }

                if (!ValidatePassword(password))
                {
                    return BadRequest("Неверный формат пароля.");
                }

                if (!ValidateName(name))
                {
                    return BadRequest("Неверный формат имени.");
                }

                if (!IsValidGender(gender))
                {
                    return BadRequest("Неверный формат пола.");
                }

                _users.Add(newUser);

                return Ok("Пользователь успешно создан.");
            }

            return Unauthorized("Неверные учетные данные.");
        }

        [HttpGet("active(5)")]
        public IActionResult GetActiveUsers(string login, string password)
        {
            

            if (CheckCredentials(login, password))
            {
                var activeUsers = _users.Where(u => u.RevokedOn == default)
                                        .OrderBy(u => u.CreatedOn)
                                        .ToList();

                return Ok(activeUsers);
            }


            return Unauthorized("Неверные учетные данные.");
        }


        [HttpGet("{login}(6)")]
        public IActionResult GetUserByLogin(string adminLogin, string adminPassword, string login)
        {
            if (CheckCredentials(adminLogin, adminPassword))
            {
                var user = _users.FirstOrDefault(u => u.Login == login);

                if (user != null && user.RevokedOn == default)
                {
                    var userResponse = new
                    {
                        Name = user.Name,
                        Gender = user.Gender,
                        Birthday = user.Birthday,
                        IsActive = true
                    };

                    return Ok(userResponse);
                }

                return NotFound("Пользователь не найден или неактивен.");
            }

            return Unauthorized("Неверные учетные данные администратора.");
        }


        [HttpGet("login(7)")]
        public IActionResult GetUserByLoginAndPassword(string login, string password)
        {
            var user = _users.FirstOrDefault(u => u.Login == login && u.Password == password);

            if (user != null && user.RevokedOn == default)
            {
                var userResponse = new
                {
                    Name = user.Name,
                    Gender = user.Gender,
                    Birthday = user.Birthday,
                    IsActive = true
                };

                return Ok(userResponse);
            }

            return NotFound("Пользователь не найден или неактивен.");
        }

        [HttpGet("olderthan/{age}(8)")]
        public IActionResult GetUsersOlderThanAge(string adminLogin, string adminPassword, int age)
        {
            if (CheckCredentials(adminLogin, adminPassword))
            {
                var usersOlderThanAge = _users.Where(u => u.Birthday.HasValue && CalculateAge(u.Birthday.Value) > age)
                                              .ToList();

                return Ok(usersOlderThanAge);
            }

            return Unauthorized("Неверные учетные данные.");
        }

        private int CalculateAge(DateTime birthday)
        {
            var today = DateTime.Today;
            var age = today.Year - birthday.Year;
            if (birthday > today.AddYears(-age))
                age--;

            return age;
        }

        [HttpDelete("{login}(9)")]
        public IActionResult DeleteUserByLogin(string adminLogin, string adminPassword, string login, bool softDelete)
        {
            if (CheckCredentials(adminLogin, adminPassword))
            {
                var user = _users.FirstOrDefault(u => u.Login == login);

                if (user != null)
                {
                    if (softDelete)
                    {
                        // Мягкое удаление - простановка RevokedOn и RevokedBy
                        user.RevokedOn = DateTime.Now;
                        user.RevokedBy = adminLogin;
                    }
                    else
                    {
                        // Полное удаление
                        _users.Remove(user);
                    }

                    return Ok("Пользователь успешно удален.");
                }

                return NotFound("Пользователь не найден.");
            }

            return Unauthorized("Неверные учетные данные.");
        }



        [HttpPut("restore/{login}(10)")]
        public IActionResult RestoreUser(string adminLogin, string adminPassword, string login)
        {
            if (CheckCredentials(adminLogin, adminPassword))
            {
                var user = _users.FirstOrDefault(u => u.Login == login);

                if (user != null)
                {
                    if (user.RevokedOn != default)
                    {
                        // Восстановление пользователя - очистка полей RevokedOn и RevokedBy
                        user.RevokedOn = default;
                        user.RevokedBy = null;

                        return Ok("Пользователь успешно восстановлен.");
                    }

                    return BadRequest("Пользователь не был удален.");
                }

                return NotFound("Пользователь не найден.");
            }

            return Unauthorized("Неверные учетные данные.");
        }

        [HttpPut("{login}(2)")]
        public IActionResult UpdateUser(string? adminLogin, string? adminPassword, string login, [FromBody] UserUpdateRequest request)
        {
            var user = _users.FirstOrDefault(u => u.Login == login);

            if (user != null)
            {
                if (request.OldPassword != user.Password)
                {
                    return BadRequest("Пароли не совпадают.");
                }

                if (!IsValidGender(request.Gender))
                {
                    return BadRequest("Неверный формат пола.");
                }

                if (!ValidateName(request.Name))
                {
                    return BadRequest("Неверный формат имени.");
                }

                if (login == user.Login && (string.IsNullOrEmpty(adminLogin) && string.IsNullOrEmpty(adminPassword) && user.RevokedOn == default) ||
                    CheckCredentials(adminLogin, adminPassword))
                {
                    user.Name = request.Name;
                    user.Gender = request.Gender;
                    user.Birthday = request.Birthday;

                    // Обновление информации о пользователе
                    user.ModifiedOn = DateTime.Now;
                    user.ModifiedBy = adminLogin ?? login;

                    return Ok("Информация о пользователе успешно обновлена.");
                }

                return Unauthorized("Недостаточно прав для обновления информации о пользователе или пользователь неактивен.");
            }

            return NotFound("Пользователь не найден.");
        }



        [HttpPut("{login}/password(3)")]
        public IActionResult UpdatePassword(string? adminLogin, string? adminPassword, string login, [FromBody] PasswordUpdateRequest request)
        {
            var user = _users.FirstOrDefault(u => u.Login == login);
            
            if (request.OldPassword != user.Password)
            {
                return BadRequest("Пароли не совпадают.");
            }

            if (!ValidatePassword(request.NewPassword))
            {
                return BadRequest("Неверный формат пароля.");
            }

           

            if (user != null)
            {
                if ((string.IsNullOrEmpty(adminLogin) && string.IsNullOrEmpty(adminPassword) && user.Login == login && user.RevokedOn == default) ||
                    CheckCredentials(adminLogin, adminPassword))
                {
                    user.Password = request.NewPassword;

                    // Обновление информации о пользователе
                    user.ModifiedOn = DateTime.Now;
                    user.ModifiedBy = CheckCredentials(adminLogin, adminPassword) ? adminLogin : user.Login;

                    return Ok("Пароль пользователя успешно обновлен.");
                }

                return Unauthorized("Неверные учетные данные или пользователь неактивен.");
            }

            return NotFound("Пользователь не найден.");
        }

        [HttpPut("{login}/login(4)")]
        public IActionResult UpdateLogin(string? adminLogin, string? adminPassword, string login, [FromBody] LoginUpdateRequest request)
        {
            var user = _users.FirstOrDefault(u => u.Login == login);

            if (!ValidateLogin(request.NewLogin))
            {
                return BadRequest("Неверный формат логина.");
            }

            if (request.OldPassword != user.Password)
            {
                return BadRequest("Пароли не совпадают.");
            }


            if (user != null)
            {
                if ((string.IsNullOrEmpty(adminLogin) && string.IsNullOrEmpty(adminPassword) && user.Login == login && user.RevokedOn == default) ||
                   CheckCredentials(adminLogin, adminPassword))
                {
                    if (_users.Any(u => u.Login == request.NewLogin))
                    {
                        return BadRequest("Указанный логин уже занят.");
                    }

                    user.Login = request.NewLogin;

                    // Обновление информации о пользователе
                    user.ModifiedOn = DateTime.Now;
                    user.ModifiedBy = CheckCredentials(adminLogin, adminPassword) ? adminLogin : user.Login;

                    return Ok("Логин пользователя успешно обновлен.");
                }

                return Unauthorized("Неверные учетные данные или пользователь неактивен.");
            }

            return NotFound("Пользователь не найден.");
        }

    }
}