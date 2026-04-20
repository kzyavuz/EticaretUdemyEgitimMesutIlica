using Core.Entities;
using Core.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configurations
{
    public class BrandConfiguration : IEntityTypeConfiguration<Brand>
    {
        public void Configure(EntityTypeBuilder<Brand> builder)
        {
            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Logo).HasMaxLength(200);

            builder.HasData(
                new Brand { Id = 1, Name = "Brand A", Description = "Description for Brand A", Logo = "logoA.png", Status = DataStatus.Active, CreatedDate = new DateTime(2026, 4, 5), UpdatedDate = new DateTime(2026, 4, 5) },
                new Brand { Id = 2, Name = "Brand B", Description = "Description for Brand B", Logo = "logoB.png", Status = DataStatus.Active, CreatedDate = new DateTime(2026, 4, 5), UpdatedDate = new DateTime(2026, 4, 5) },
                new Brand { Id = 3, Name = "Brand C", Description = "Description for Brand C", Logo = "logoC.png", Status = DataStatus.Active, CreatedDate = new DateTime(2026, 4, 5), UpdatedDate = new DateTime(2026, 4, 5) }
            );
        }
    }
}
