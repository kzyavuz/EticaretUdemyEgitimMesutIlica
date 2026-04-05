using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.Property(x => x.Title).IsRequired().HasMaxLength(250);
            builder.Property(x => x.ProductCode).HasMaxLength(60);
            builder.Property(x => x.Image).HasMaxLength(100);
        }
    }
}
