using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using trucki.CustomExtension;
using trucki.DatabaseContext;
using trucki.Entities;
using trucki.Interfaces.IRepository;
using trucki.Interfaces.IServices;
using trucki.Models.RequestModel;
using trucki.Models.ResponseModels;

namespace trucki.Repository;

public class DriverRepository:IDriverRepository
{
    private readonly TruckiDBContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly IAuthService _authService;
    private readonly IUploadService _uploadService;
    private readonly IEmailService _emailSender;

    public DriverRepository(TruckiDBContext appDbContext, UserManager<User> userManager, IMapper mapper,
        IAuthService authService, IUploadService uploadService, IEmailService emailSender)
    {
        _context = appDbContext;
        _mapper = mapper;
        _authService = authService;
        _uploadService = uploadService;
        _emailSender = emailSender;
        _userManager = userManager;
    }
     public async Task<ApiResponseModel<string>> AddDriver(AddDriverRequestModel model)
    {
        var existingManager = await _context.Drivers
            .FirstOrDefaultAsync(m => m.EmailAddress == model.Email || m.Phone == model.Number);

        if (existingManager != null)
        {
            if (existingManager.EmailAddress == model.Email)
            {
                return new ApiResponseModel<string>
                {
                    IsSuccessful = false,
                    Message = "Email address already exists",
                    StatusCode = 400 // Bad Request
                };
            }
            else
            {
                return new ApiResponseModel<string>
                {
                    IsSuccessful = false,
                    Message = "Phone number already exists",
                    StatusCode = 400 // Bad Request
                };
            }
        }

        var newDriver = new Driver
        {
            Name = model.Name,
            Phone = model.Number,
            EmailAddress = model.Email,
            TruckId = model.TruckId,
            //PassportFile = "",
            //DriversLicence = ""
        };

        var imagePath = "";
        if (model.IdCard != null && model.IdCard.Length > 0)
        {
            // Save Id card 
            imagePath = await _uploadService.UploadFile(model.IdCard, $"{newDriver.Name}userIdCard");
        }

        // Id card
        newDriver.DriversLicence = imagePath;

        var profilePicPath = "";
        if (model.Picture != null && model.Picture.Length > 0)
        {
            // save profile picture
            profilePicPath = await _uploadService.UploadFile(model.Picture, $"{newDriver.Name}userProfilePicture");
        }


        // Profile picture
        newDriver.PassportFile = profilePicPath;


        _context.Drivers.Add(newDriver);
        var password = HelperClass.GenerateRandomPassword();
        var res = await _authService.AddNewUserAsync(newDriver.Name, newDriver.EmailAddress,
            "driver", password);
        //TODO:: Email password to user
        if (res.StatusCode == 201)
        {
            // **Save changes to database**
            await _context.SaveChangesAsync();
            return new ApiResponseModel<string>
            {
                IsSuccessful = true,
                Message = "Driver created successfully",
                StatusCode = 201,
                Data = password
            };
        }

        return new ApiResponseModel<string>
        {
            IsSuccessful = false,
            Message = "An error occurred while creating the manager",
            StatusCode = 400, // Bad Request
        };
    }

    public async Task<ApiResponseModel<bool>> EditDriver(EditDriverRequestModel model)
    {
        var driver = await _context.Drivers.FindAsync(model.Id);

        if (driver == null)
        {
            return new ApiResponseModel<bool>
            {
                IsSuccessful = false,
                Message = "Driver not found",
                StatusCode = 404
            };
        }

        driver.Name = model.Name;
        driver.Phone = model.Number;

        var imagePath = "";
        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            imagePath = await _uploadService.UploadFile(model.ProfilePicture, $"{driver.Name}userIdCard");
        }

        driver.PassportFile = imagePath;

        // Save changes to database
        _context.Drivers.Update(driver);
        await _context.SaveChangesAsync();

        return new ApiResponseModel<bool>
        {
            IsSuccessful = true,
            Message = "Driver updated successfully",
            StatusCode = 200,
            Data = true
        };
    }

    public async Task<ApiResponseModel<bool>> DeactivateDriver(string driverId)
    {
        var driver = await _context.Drivers.FindAsync(driverId);
        if (driver == null)
        {
            return new ApiResponseModel<bool>
            {
                IsSuccessful = false,
                Message = "Driver not found",
                StatusCode = 404 // Not Found
            };
        }
        
        driver.IsActive = false;
        _context.Drivers.Update(driver);
        await _context.SaveChangesAsync();
        return new ApiResponseModel<bool>
        {
            IsSuccessful = true,
            Message = "Driver deactivated successfully",
            StatusCode = 200,
            Data = true
        };
    }
        public async Task<ApiResponseModel<List<AllDriverResponseModel>>> GetAllDrivers()
    {
        var drivers = await _context.Drivers.ToListAsync();

        var driverResponseModels = _mapper.Map<List<AllDriverResponseModel>>(drivers);

        return new ApiResponseModel<List<AllDriverResponseModel>>
        {
            IsSuccessful = true,
            Message = "Drivers retrieved successfully",
            StatusCode = 200,
            Data = driverResponseModels
        };
    }

    public async Task<ApiResponseModel<DriverResponseModel>> GetDriverById(string id)
    {
        var driver = await _context.Drivers.Where(x => x.Id == id).Include(d => d.Truck).FirstOrDefaultAsync();

        if (driver == null)
            return new ApiResponseModel<DriverResponseModel>
            {
                Data = new DriverResponseModel { }, IsSuccessful = false, Message = "No manager found",
                StatusCode = 404
            };

        var driverToReturn = _mapper.Map<DriverResponseModel>(driver);
        if (driver.Truck != null)
        {
            driverToReturn.Truck = _mapper.Map<AllTruckResponseModel>(driver.Truck); 
        }

        return new ApiResponseModel<DriverResponseModel>
        {
            IsSuccessful = true,
            Message = "Driver retrieved successfully",
            StatusCode = 200,
            Data = driverToReturn
        };
    }

    public async Task<ApiResponseModel<IEnumerable<AllDriverResponseModel>>> SearchDrivers(string searchWords)
    {
        IQueryable<Driver> query = _context.Drivers;

        if (!string.IsNullOrEmpty(searchWords) && searchWords != "" && searchWords != " " &&
            searchWords.ToLower() != "null")
        {
            query = query.Where(d => d.Name.ToLower().Contains(searchWords.ToLower()));
        }

        var totalItems = await query.CountAsync();

        var drivers = await query.ToListAsync();

        if (!drivers.Any())
        {
            return new ApiResponseModel<IEnumerable<AllDriverResponseModel>>
            {
                Data = new List<AllDriverResponseModel> { },
                IsSuccessful = false,
                Message = "No driver found",
                StatusCode = 404
            };
        }

        var data = _mapper.Map<IEnumerable<AllDriverResponseModel>>(drivers);

        return new ApiResponseModel<IEnumerable<AllDriverResponseModel>>
        {
            Data = data,
            IsSuccessful = true,
            Message = "Drivers successfully retrieved",
            StatusCode = 200,
        };
    }
    

}