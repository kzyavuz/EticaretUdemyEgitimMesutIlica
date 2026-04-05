using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Configurations
{

    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.Property(x => x.Title).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Image).HasMaxLength(200);

            builder.HasData(
                new Category { Id = 1, Title = "Elektronik", Description = "Elektronik ürünler", Image = "elektronik.jpg", ISTopMenu = true, IsActive = true, CreatedDate = new DateTime(2026, 4, 5), UpdatedDate = new DateTime(2026, 4, 5) },
                new Category { Id = 2, Title = "Giyim", Description = "Giyim ürünleri", Image = "giyim.jpg", ISTopMenu = true, IsActive = true, CreatedDate = new DateTime(2026, 4, 5), UpdatedDate = new DateTime(2026, 4, 5) },
                new Category { Id = 3, Title = "Ev & Yaşam", Description = "Ev ve yaşam ürünleri", Image = "ev-yaşam.jpg", ISTopMenu = false, IsActive = true, CreatedDate = new DateTime(2026, 4, 5), UpdatedDate = new DateTime(2026, 4, 5) },
                new Category { Id = 4, Title = "Spor & Outdoor", Description = "Spor ve outdoor ürünleri", Image = "spor-outdoor.jpg", ISTopMenu = false, IsActive = false, CreatedDate = new DateTime(2026, 4, 5), UpdatedDate = new DateTime(2026, 4, 5) }
            );
        }
    }
}
