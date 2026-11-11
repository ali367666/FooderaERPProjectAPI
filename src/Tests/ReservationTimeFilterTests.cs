using Domain.Entities;
using Infrastructure.Persistence.Context;
using Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

/// <summary>The reservation list can be narrowed to a time range, like the old receipts.</summary>
public class ReservationTimeFilterTests
{
    private const int Company = 1;

    private static (AppDbContext Db, ReservationRepository Repo) Create()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Restaurants.Add(new Restaurant { Id = 1, CompanyId = Company, Name = "F" });
        // 4 Nov: 12:00, 14:30, 19:00;  5 Nov: 09:00, 20:00
        AddResv(db, 1, new DateTime(2026, 11, 4), new TimeSpan(12, 0, 0));
        AddResv(db, 2, new DateTime(2026, 11, 4), new TimeSpan(14, 30, 0));
        AddResv(db, 3, new DateTime(2026, 11, 4), new TimeSpan(19, 0, 0));
        AddResv(db, 4, new DateTime(2026, 11, 5), new TimeSpan(9, 0, 0));
        AddResv(db, 5, new DateTime(2026, 11, 5), new TimeSpan(20, 0, 0));
        db.SaveChanges();
        return (db, new ReservationRepository(db));
    }

    private static void AddResv(AppDbContext db, int id, DateTime day, TimeSpan time)
        => db.Reservations.Add(new Reservation { Id = id, CompanyId = Company, RestaurantId = 1, GuestName = "A", GuestPhone = "1", ReservationDate = day, ReservationTime = time });

    private static int[] Ids(List<Reservation> list) => list.Select(r => r.Id).OrderBy(i => i).ToArray();

    [Fact]
    public async Task One_day_in_a_time_range()
    {
        var (_, repo) = Create();

        var list = await repo.GetAllAsync(Company, new DateTime(2026, 11, 4), null, null, new TimeSpan(14, 0, 0), new TimeSpan(19, 0, 0), default);

        Assert.Equal(new[] { 2, 3 }, Ids(list)); // 19:00 is inside the 19:00 minute
    }

    [Fact]
    public async Task A_range_of_days_runs_from_the_first_days_start_to_the_last_days_end()
    {
        var (_, repo) = Create();

        var list = await repo.GetAllAsync(Company, null, new DateTime(2026, 11, 4), new DateTime(2026, 11, 5), new TimeSpan(14, 0, 0), new TimeSpan(9, 0, 0), default);

        Assert.Equal(new[] { 2, 3, 4 }, Ids(list)); // from 4 Nov 14:00 until 5 Nov 09:00 (the 09:00 minute included)
    }

    [Fact]
    public async Task Without_a_time_the_whole_days_are_listed()
    {
        var (_, repo) = Create();

        Assert.Equal(new[] { 1, 2, 3 }, Ids(await repo.GetAllAsync(Company, new DateTime(2026, 11, 4), null, null, null, null, default)));
    }

    [Fact]
    public async Task Without_a_day_the_time_of_day_applies_to_every_day()
    {
        var (_, repo) = Create();

        var list = await repo.GetAllAsync(Company, null, null, null, new TimeSpan(18, 0, 0), new TimeSpan(21, 0, 0), default);

        Assert.Equal(new[] { 3, 5 }, Ids(list));
    }

    [Fact]
    public async Task An_end_of_23_59_runs_to_the_end_of_the_day()
    {
        var (_, repo) = Create();

        var list = await repo.GetAllAsync(Company, new DateTime(2026, 11, 5), null, null, new TimeSpan(10, 0, 0), new TimeSpan(23, 59, 0), default);

        Assert.Equal(new[] { 5 }, Ids(list));
    }
}
