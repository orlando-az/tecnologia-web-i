using System;
using DeliveryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Data;

public class AppDbContext:DbContext
{
    // * Conexion
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
    {}

    // Tables

    public DbSet<Product> Products {get;set;}
}
