using Microsoft.EntityFrameworkCore;
using PetClinic.Data;
using PetClinic.Models;
using PetClinic.Services;

namespace PetClinic.Tests.Services;

public class VisitServiceTests : IDisposable
{
    private readonly PetClinicContext _db;
    private readonly VisitService _sut;

    public VisitServiceTests()
    {
        var options = new DbContextOptionsBuilder<PetClinicContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _db = new PetClinicContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _sut = new VisitService(_db);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    [Fact]
    public async Task CreateAsync_PersistsVisitToDatabase()
    {
        var petType = new PetType { Name = "Dog" };
        _db.PetTypes.Add(petType);
        var owner = new Owner
        {
            FirstName = "John",
            LastName = "Doe",
            Address = "123 Main St",
            City = "Springfield",
            Telephone = "1234567890"
        };
        _db.Owners.Add(owner);
        await _db.SaveChangesAsync();

        var pet = new Pet
        {
            Name = "Buddy",
            BirthDate = new DateOnly(2020, 1, 1),
            TypeId = petType.Id,
            OwnerId = owner.Id
        };
        _db.Pets.Add(pet);
        await _db.SaveChangesAsync();

        var visit = new Visit
        {
            VisitDate = new DateOnly(2024, 6, 15),
            Description = "Annual checkup",
            PetId = pet.Id
        };

        await _sut.CreateAsync(visit);

        var saved = await _db.Visits.SingleAsync(v => v.Id == visit.Id);
        Assert.Equal("Annual checkup", saved.Description);
        Assert.Equal(new DateOnly(2024, 6, 15), saved.VisitDate);
        Assert.Equal(pet.Id, saved.PetId);
    }

    [Fact]
    public async Task CreateAsync_AssignsIdToVisit()
    {
        var petType = new PetType { Name = "Cat" };
        _db.PetTypes.Add(petType);
        var owner = new Owner
        {
            FirstName = "Jane",
            LastName = "Smith",
            Address = "456 Elm St",
            City = "Shelbyville",
            Telephone = "0987654321"
        };
        _db.Owners.Add(owner);
        await _db.SaveChangesAsync();

        var pet = new Pet
        {
            Name = "Whiskers",
            BirthDate = new DateOnly(2021, 3, 10),
            TypeId = petType.Id,
            OwnerId = owner.Id
        };
        _db.Pets.Add(pet);
        await _db.SaveChangesAsync();

        var visit = new Visit
        {
            VisitDate = DateOnly.FromDateTime(DateTime.Today),
            Description = "Vaccination",
            PetId = pet.Id
        };

        await _sut.CreateAsync(visit);

        Assert.True(visit.Id > 0);
    }

    [Fact]
    public async Task CreateAsync_MultipleVisitsForSamePet_AllPersisted()
    {
        var petType = new PetType { Name = "Bird" };
        _db.PetTypes.Add(petType);
        var owner = new Owner
        {
            FirstName = "Alice",
            LastName = "Brown",
            Address = "789 Oak Ave",
            City = "Capital City",
            Telephone = "5551234567"
        };
        _db.Owners.Add(owner);
        await _db.SaveChangesAsync();

        var pet = new Pet
        {
            Name = "Tweety",
            BirthDate = new DateOnly(2022, 5, 20),
            TypeId = petType.Id,
            OwnerId = owner.Id
        };
        _db.Pets.Add(pet);
        await _db.SaveChangesAsync();

        var visit1 = new Visit { VisitDate = new DateOnly(2024, 1, 10), Description = "First visit", PetId = pet.Id };
        var visit2 = new Visit { VisitDate = new DateOnly(2024, 6, 20), Description = "Second visit", PetId = pet.Id };

        await _sut.CreateAsync(visit1);
        await _sut.CreateAsync(visit2);

        var visits = await _db.Visits.Where(v => v.PetId == pet.Id).ToListAsync();
        Assert.Equal(2, visits.Count);
        Assert.Contains(visits, v => v.Description == "First visit");
        Assert.Contains(visits, v => v.Description == "Second visit");
    }
}
