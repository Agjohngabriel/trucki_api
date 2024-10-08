using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using trucki.DatabaseContext;
using trucki.Entities;
using trucki.Interfaces.IRepository;
using trucki.Interfaces.IServices;
using trucki.Models.RequestModel;
using trucki.Models.ResponseModels;

namespace trucki.Repository;

public class TruckOwnerRepository:ITruckOwnerRepository
{
    private readonly TruckiDBContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly IAuthService _authService;
    private readonly IUploadService _uploadService;
    private readonly IEmailService _emailSender;

    public TruckOwnerRepository(TruckiDBContext appDbContext, UserManager<User> userManager, IMapper mapper,
        IAuthService authService, IUploadService uploadService, IEmailService emailSender)
    {
        _context = appDbContext;
        _mapper = mapper;
        _authService = authService;
        _uploadService = uploadService;
        _emailSender = emailSender;
        _userManager = userManager;
    }
    
    public async Task<ApiResponseModel<bool>> CreateNewTruckOwner(AddTruckOwnerRequestBody model)
    {
        // Create a new TruckiOwner instance
        var bankDetails = new BankDetails
        {
            BankName = model.BankName,
            BankAccountNumber = model.BankAccountNumber,
            BankAccountName = model.BankAccountName
        };

        _context.BankDetails.Add(bankDetails);
        await _context.SaveChangesAsync();
        var newOwner = new TruckOwner
        {
            Name = model.Name,
            EmailAddress = model.EmailAddress,
            Phone = model.Phone,
            Address = model.Address,
            BankDetailsId = bankDetails.Id, // Associate the new owner with the created bank details
            BankDetails = bankDetails
        };

        // Add owner to context and save changes
        

        newOwner.IdCardUrl = model.IdCard;
        newOwner.ProfilePictureUrl = model.ProfilePicture;
        _context.TruckOwners.Add(newOwner);
        await _context.SaveChangesAsync();

        return new ApiResponseModel<bool>
        {
            IsSuccessful = true,
            Message = "Truck owner created successfully",
            StatusCode = 201,
            Data = true
        };
    }

    public async Task<ApiResponseModel<TruckOwnerResponseModel>> GetTruckOwnerById(string id)
    {
        var owner = await _context.TruckOwners
            .Include(o => o.trucks) // Eagerly load the trucks collection
            .Include(o => o.BankDetails) // Eagerly load the trucks collection
            .FirstOrDefaultAsync(o => o.Id == id);

        if (owner == null)
        {
            return new ApiResponseModel<TruckOwnerResponseModel>
            {
                IsSuccessful = false,
                Message = "Truck owner not found",
                StatusCode = 404
            };
        }

        // Manual mapping to ensure correct population
        var responseModel = new TruckOwnerResponseModel
        {
            Id = owner.Id,
            Name = owner.Name,
            EmailAddress = owner.EmailAddress,
            Phone = owner.Phone,
            Address = owner.Address,
            IdCardUrl = owner.IdCardUrl,
            ProfilePictureUrl = owner.ProfilePictureUrl,
            noOfTrucks = owner.trucks.Count.ToString(),  
            BankDetails = _mapper.Map<BankDetailsResponseModel>(owner.BankDetails), 
            CreatedAt = owner.CreatedAt,
            UpdatedAt = owner.UpdatedAt
        };

        return new ApiResponseModel<TruckOwnerResponseModel>
        {
            IsSuccessful = true,
            StatusCode = 200,
            Message = "Success",
            Data = responseModel
        };
    }

    public async Task<ApiResponseModel<bool>> EditTruckOwner(EditTruckOwnerRequestBody model)
    {
        // Find the owner to update
        var owner = await _context.TruckOwners.FindAsync(model.Id);

        // Check if owner exists
        if (owner == null)
        {
            return new ApiResponseModel<bool>
            {
                IsSuccessful = false,
                Message = "Truck owner not found",
                StatusCode = 404
            };
        }

        // Update properties directly on the existing instance
        owner.Name = model.Name;
        owner.EmailAddress = model.EmailAddress;
        owner.Phone = model.Phone;
        owner.Address = model.Address;

        // Save changes to database
        await _context.SaveChangesAsync();

        return new ApiResponseModel<bool>
        {
            IsSuccessful = true,
            Message = "Truck owner updated successfully",
            StatusCode = 200,
            Data = true
        };
    }

    public async Task<ApiResponseModel<bool>> DeleteTruckOwner(string id)
    {
        // Find the owner to delete
        var owner = await _context.TruckOwners.FindAsync(id);

        // Check if owner exists
        if (owner == null)
        {
            return new ApiResponseModel<bool>
            {
                IsSuccessful = false,
                Message = "Truck owner not found",
                StatusCode = 404
            };
        }

        // Remove owner from context and save changes
        _context.TruckOwners.Remove(owner);
        await _context.SaveChangesAsync();

        return new ApiResponseModel<bool>
        {
            IsSuccessful = true,
            Message = "Truck owner deleted successfully",
            StatusCode = 200,
            Data = true
        };
    }

    public async Task<ApiResponseModel<List<AllTruckOwnerResponseModel>>> GetAllTruckOwners()
    {
        // Fetch all truck owners from the database
        var owners = await _context.TruckOwners.ToListAsync();
        var result = _mapper.Map<List<AllTruckOwnerResponseModel>>(owners);
        return new ApiResponseModel<List<AllTruckOwnerResponseModel>>
        {
            IsSuccessful = true,
            StatusCode = 200,
            Data = result
        };
    }
    
    public async Task<ApiResponseModel<IEnumerable<AllTruckOwnerResponseModel>>> SearchTruckOwners(string searchWords)
    {
        IQueryable<TruckOwner> query = _context.TruckOwners;

        if (!string.IsNullOrEmpty(searchWords) && searchWords != "" && searchWords != " " &&
            searchWords.ToLower() != "null")
        {
            query = query.Where(d => d.Name.ToLower().Contains(searchWords.ToLower()));
        }

        var totalItems = await query.CountAsync();

        var owners = await query.ToListAsync();

        if (!owners.Any())
        {
            return new ApiResponseModel<IEnumerable<AllTruckOwnerResponseModel>>
            {
                Data = new List<AllTruckOwnerResponseModel> { },
                IsSuccessful = false,
                Message = "No Truck Owner found",
                StatusCode = 404
            };
        }

        var data = _mapper.Map<IEnumerable<AllTruckOwnerResponseModel>>(owners);

        return new ApiResponseModel<IEnumerable<AllTruckOwnerResponseModel>>
        {
            Data = data,
            IsSuccessful = true,
            Message = "Truck Owner successfully retrieved",
            StatusCode = 200,
        };
    }

    
}