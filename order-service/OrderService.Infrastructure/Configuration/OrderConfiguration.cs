using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Configuration;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.DeliveryAddress)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Status)
            .IsRequired();

        builder.Property(o => o.DeliveryLatitude)
            .IsRequired();

        builder.Property(o => o.DeliveryLongitude)
            .IsRequired();

        // AssignedAt is already in the database, so we just configure it
        builder.Property(o => o.AssignedAt);

        builder.Property(o => o.DriverId);
    }
}