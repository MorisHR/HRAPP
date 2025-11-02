using HRMS.Core.Entities.Master;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HRMS.Infrastructure.Data;

/// <summary>
/// Seed data for Mauritius Industry Sectors and Compliance Rules
/// Based on Mauritius Remuneration Orders 2025
/// Last Updated: November 2025
/// </summary>
public static class SectorSeedData
{
    /// <summary>
    /// Seeds all 30+ Mauritius industry sectors with hierarchical relationships
    /// </summary>
    public static void SeedIndustrySectors(MasterDbContext context)
    {
        if (context.IndustrySectors.Any())
        {
            Console.WriteLine("[INFO] Industry sectors already seeded. Skipping...");
            return;
        }

        Console.WriteLine("[INFO] Seeding Mauritius industry sectors...");

        var sectors = new List<IndustrySector>();
        var sectorId = 1;

        // ===== 1. CATERING & TOURISM INDUSTRIES =====
        var cateringTourismId = sectorId++;
        sectors.Add(new IndustrySector
        {
            Id = cateringTourismId,
            SectorCode = "CAT",
            SectorName = "Catering & Tourism Industries",
            SectorNameFrench = "Industries de l'Hôtellerie et du Tourisme",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 185 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // Sub-sectors: Catering & Tourism
        sectors.AddRange(new[]
        {
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "CAT-HOTEL-LARGE",
                SectorName = "Hotels & Accommodation (40+ covers)",
                SectorNameFrench = "Hôtels et Hébergement (40+ couverts)",
                ParentSectorId = cateringTourismId,
                RemunerationOrderReference = "GN No. 185 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "CAT-HOTEL-SMALL",
                SectorName = "Hotels & Accommodation (Under 40 covers)",
                SectorNameFrench = "Hôtels et Hébergement (Moins de 40 couverts)",
                ParentSectorId = cateringTourismId,
                RemunerationOrderReference = "GN No. 185 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "CAT-RESTAURANT-LARGE",
                SectorName = "Restaurants & Cafés (40+ covers)",
                SectorNameFrench = "Restaurants et Cafés (40+ couverts)",
                ParentSectorId = cateringTourismId,
                RemunerationOrderReference = "GN No. 185 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "CAT-RESTAURANT-SMALL",
                SectorName = "Restaurants & Cafés (Under 40 covers)",
                SectorNameFrench = "Restaurants et Cafés (Moins de 40 couverts)",
                ParentSectorId = cateringTourismId,
                RemunerationOrderReference = "GN No. 185 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "CAT-FASTFOOD",
                SectorName = "Fast Food Outlets",
                SectorNameFrench = "Restauration Rapide",
                ParentSectorId = cateringTourismId,
                RemunerationOrderReference = "GN No. 185 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "CAT-BARS",
                SectorName = "Bars & Night Clubs",
                SectorNameFrench = "Bars et Boîtes de Nuit",
                ParentSectorId = cateringTourismId,
                RemunerationOrderReference = "GN No. 185 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "CAT-BOARDINGHOUSE",
                SectorName = "Boarding Houses & Guest Houses",
                SectorNameFrench = "Pensions et Maisons d'Hôtes",
                ParentSectorId = cateringTourismId,
                RemunerationOrderReference = "GN No. 185 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "CAT-ATTRACTIONS",
                SectorName = "Tourist Attractions & Leisure Parks",
                SectorNameFrench = "Attractions Touristiques et Parcs de Loisirs",
                ParentSectorId = cateringTourismId,
                RemunerationOrderReference = "GN No. 185 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        });

        // ===== 2. CONSTRUCTION & QUARRYING =====
        var constructionId = sectorId++;
        sectors.Add(new IndustrySector
        {
            Id = constructionId,
            SectorCode = "CONST",
            SectorName = "Construction & Quarrying Industries",
            SectorNameFrench = "Industries de la Construction et des Carrières",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 162 of 2022",
            RemunerationOrderYear = 2022,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 3. BUSINESS PROCESS OUTSOURCING (BPO) =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "BPO",
            SectorName = "Business Process Outsourcing (BPO)",
            SectorNameFrench = "Externalisation des Processus d'Affaires",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 201 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 4. SECURITY SERVICES =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "SECURITY",
            SectorName = "Security Services",
            SectorNameFrench = "Services de Sécurité",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 178 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = true, // Requires police clearance
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 5. CLEANING SERVICES =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "CLEANING",
            SectorName = "Cleaning Services",
            SectorNameFrench = "Services de Nettoyage",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 165 of 2022",
            RemunerationOrderYear = 2022,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 6. FINANCIAL SERVICES =====
        var financialId = sectorId++;
        sectors.Add(new IndustrySector
        {
            Id = financialId,
            SectorCode = "FINANCE",
            SectorName = "Financial Services",
            SectorNameFrench = "Services Financiers",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 189 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = true, // FSC licensing
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // Sub-sectors: Financial Services
        sectors.AddRange(new[]
        {
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "FINANCE-BANKING",
                SectorName = "Banking",
                SectorNameFrench = "Services Bancaires",
                ParentSectorId = financialId,
                RemunerationOrderReference = "GN No. 189 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "FINANCE-INSURANCE",
                SectorName = "Insurance",
                SectorNameFrench = "Assurance",
                ParentSectorId = financialId,
                RemunerationOrderReference = "GN No. 189 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "FINANCE-FUND",
                SectorName = "Fund Administration",
                SectorNameFrench = "Administration de Fonds",
                ParentSectorId = financialId,
                RemunerationOrderReference = "GN No. 189 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "FINANCE-WEALTH",
                SectorName = "Wealth Management",
                SectorNameFrench = "Gestion de Patrimoine",
                ParentSectorId = financialId,
                RemunerationOrderReference = "GN No. 189 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        });

        // ===== 7. ICT & SOFTWARE DEVELOPMENT =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "ICT",
            SectorName = "Information & Communication Technology",
            SectorNameFrench = "Technologies de l'Information et de la Communication",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 195 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 8. MANUFACTURING =====
        var manufacturingId = sectorId++;
        sectors.Add(new IndustrySector
        {
            Id = manufacturingId,
            SectorCode = "MANUFACTURING",
            SectorName = "Manufacturing Industries",
            SectorNameFrench = "Industries Manufacturières",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 172 of 2022",
            RemunerationOrderYear = 2022,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // Sub-sectors: Manufacturing
        sectors.AddRange(new[]
        {
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "MFG-TEXTILE",
                SectorName = "Textiles & Apparel (Export Enterprises)",
                SectorNameFrench = "Textile et Habillement (Entreprises d'Exportation)",
                ParentSectorId = manufacturingId,
                RemunerationOrderReference = "GN No. 172 of 2022",
                RemunerationOrderYear = 2022,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "MFG-JEWELRY",
                SectorName = "Jewelry & Optical Goods",
                SectorNameFrench = "Bijouterie et Optique",
                ParentSectorId = manufacturingId,
                RemunerationOrderReference = "GN No. 172 of 2022",
                RemunerationOrderYear = 2022,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "MFG-FURNITURE",
                SectorName = "Furniture Manufacturing",
                SectorNameFrench = "Fabrication de Meubles",
                ParentSectorId = manufacturingId,
                RemunerationOrderReference = "GN No. 172 of 2022",
                RemunerationOrderYear = 2022,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "MFG-FOOD",
                SectorName = "Food Processing & Beverages",
                SectorNameFrench = "Transformation Alimentaire et Boissons",
                ParentSectorId = manufacturingId,
                RemunerationOrderReference = "GN No. 172 of 2022",
                RemunerationOrderYear = 2022,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        });

        // ===== 9. RETAIL & DISTRIBUTIVE TRADE =====
        var retailId = sectorId++;
        sectors.Add(new IndustrySector
        {
            Id = retailId,
            SectorCode = "RETAIL",
            SectorName = "Retail & Distributive Trade",
            SectorNameFrench = "Commerce de Détail et Distribution",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 183 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // Sub-sectors: Retail
        sectors.AddRange(new[]
        {
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "RETAIL-SUPERMARKET",
                SectorName = "Supermarkets & Hypermarkets",
                SectorNameFrench = "Supermarchés et Hypermarchés",
                ParentSectorId = retailId,
                RemunerationOrderReference = "GN No. 183 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "RETAIL-SHOPS",
                SectorName = "Shops & Stores",
                SectorNameFrench = "Boutiques et Magasins",
                ParentSectorId = retailId,
                RemunerationOrderReference = "GN No. 183 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "RETAIL-WHOLESALE",
                SectorName = "Wholesale Trade",
                SectorNameFrench = "Commerce de Gros",
                ParentSectorId = retailId,
                RemunerationOrderReference = "GN No. 183 of 2023",
                RemunerationOrderYear = 2023,
                IsActive = true,
                RequiresSpecialPermits = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        });

        // ===== 10. HEALTHCARE & MEDICAL SERVICES =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "HEALTHCARE",
            SectorName = "Healthcare & Medical Services",
            SectorNameFrench = "Services de Santé et Médicaux",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 191 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = true, // Medical licensing
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 11. EDUCATION & TRAINING =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "EDUCATION",
            SectorName = "Education & Training Services",
            SectorNameFrench = "Services d'Éducation et de Formation",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 187 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 12. TRANSPORT & LOGISTICS =====
        var transportId = sectorId++;
        sectors.Add(new IndustrySector
        {
            Id = transportId,
            SectorCode = "TRANSPORT",
            SectorName = "Transport & Logistics",
            SectorNameFrench = "Transport et Logistique",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 174 of 2022",
            RemunerationOrderYear = 2022,
            IsActive = true,
            RequiresSpecialPermits = true, // Driver's license, goods carrier permit
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // Sub-sectors: Transport
        sectors.AddRange(new[]
        {
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "TRANSPORT-FREIGHT",
                SectorName = "Freight & Cargo Services",
                SectorNameFrench = "Services de Fret et Cargaison",
                ParentSectorId = transportId,
                RemunerationOrderReference = "GN No. 174 of 2022",
                RemunerationOrderYear = 2022,
                IsActive = true,
                RequiresSpecialPermits = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "TRANSPORT-TAXI",
                SectorName = "Taxi Services",
                SectorNameFrench = "Services de Taxi",
                ParentSectorId = transportId,
                RemunerationOrderReference = "GN No. 174 of 2022",
                RemunerationOrderYear = 2022,
                IsActive = true,
                RequiresSpecialPermits = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new IndustrySector
            {
                Id = sectorId++,
                SectorCode = "TRANSPORT-BUS",
                SectorName = "Bus Services",
                SectorNameFrench = "Services de Bus",
                ParentSectorId = transportId,
                RemunerationOrderReference = "GN No. 174 of 2022",
                RemunerationOrderYear = 2022,
                IsActive = true,
                RequiresSpecialPermits = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        });

        // ===== 13. BAKERIES =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "BAKERY",
            SectorName = "Bakeries & Confectionery",
            SectorNameFrench = "Boulangeries et Confiseries",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 168 of 2022",
            RemunerationOrderYear = 2022,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 14. CINEMA & ENTERTAINMENT =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "ENTERTAINMENT",
            SectorName = "Cinema & Entertainment",
            SectorNameFrench = "Cinéma et Divertissement",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 179 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 15. LEGAL SERVICES =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "LEGAL",
            SectorName = "Legal Services (Attorneys & Notaries)",
            SectorNameFrench = "Services Juridiques (Avocats et Notaires)",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 193 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = true, // Bar admission
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 16. REAL ESTATE & PROPERTY MANAGEMENT =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "REALESTATE",
            SectorName = "Real Estate & Property Management",
            SectorNameFrench = "Immobilier et Gestion de Propriétés",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 186 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 17. AGRICULTURE & FISHING =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "AGRICULTURE",
            SectorName = "Agriculture & Fishing",
            SectorNameFrench = "Agriculture et Pêche",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 161 of 2022",
            RemunerationOrderYear = 2022,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 18. PRINTING & PUBLISHING =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "PRINTING",
            SectorName = "Printing & Publishing",
            SectorNameFrench = "Imprimerie et Édition",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 177 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 19. TELECOMMUNICATIONS =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "TELECOM",
            SectorName = "Telecommunications",
            SectorNameFrench = "Télécommunications",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 197 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = true, // ICTA licensing
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 20. IMPORT/EXPORT TRADE =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "IMPORTEXPORT",
            SectorName = "Import/Export Trade",
            SectorNameFrench = "Commerce d'Importation/Exportation",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 182 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 21. WAREHOUSE & STORAGE =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "WAREHOUSE",
            SectorName = "Warehouse & Storage Services",
            SectorNameFrench = "Services d'Entreposage et de Stockage",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 175 of 2022",
            RemunerationOrderYear = 2022,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 22. PROFESSIONAL SERVICES =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "PROFESSIONAL",
            SectorName = "Professional Services (Accounting, Consulting)",
            SectorNameFrench = "Services Professionnels (Comptabilité, Conseil)",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 192 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 23. HOSPITALITY SERVICES =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "HOSPITALITY",
            SectorName = "Hospitality Services (excluding Catering)",
            SectorNameFrench = "Services d'Accueil (hors Restauration)",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 188 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 24. BEAUTY & WELLNESS SERVICES =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "BEAUTY",
            SectorName = "Beauty & Wellness Services",
            SectorNameFrench = "Services de Beauté et de Bien-être",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 169 of 2022",
            RemunerationOrderYear = 2022,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 25. EVENT MANAGEMENT =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "EVENTS",
            SectorName = "Event Management & Wedding Services",
            SectorNameFrench = "Gestion d'Événements et Services de Mariage",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 184 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 26. DOMESTIC SERVICES =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "DOMESTIC",
            SectorName = "Domestic Services",
            SectorNameFrench = "Services Domestiques",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 166 of 2022",
            RemunerationOrderYear = 2022,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 27. GENERAL OFFICE/ADMINISTRATIVE =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "GENERAL",
            SectorName = "General Office & Administrative Services",
            SectorNameFrench = "Services Généraux de Bureau et Administratifs",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 199 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 28. PHARMACY & PHARMACEUTICAL =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "PHARMACY",
            SectorName = "Pharmacy & Pharmaceutical Services",
            SectorNameFrench = "Pharmacie et Services Pharmaceutiques",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 190 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = true, // Pharmacy Council registration
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 29. AUTOMOTIVE SERVICES =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "AUTOMOTIVE",
            SectorName = "Automotive Services & Repairs",
            SectorNameFrench = "Services Automobiles et Réparations",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 170 of 2022",
            RemunerationOrderYear = 2022,
            IsActive = true,
            RequiresSpecialPermits = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== 30. RECRUITMENT & EMPLOYMENT AGENCIES =====
        sectors.Add(new IndustrySector
        {
            Id = sectorId++,
            SectorCode = "RECRUITMENT",
            SectorName = "Recruitment & Employment Agencies",
            SectorNameFrench = "Agences de Recrutement et d'Emploi",
            ParentSectorId = null,
            RemunerationOrderReference = "GN No. 194 of 2023",
            RemunerationOrderYear = 2023,
            IsActive = true,
            RequiresSpecialPermits = true, // Labour licensing
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        context.IndustrySectors.AddRange(sectors);
        context.SaveChanges();

        Console.WriteLine($"[SUCCESS] Seeded {sectors.Count} industry sectors");
    }

    /// <summary>
    /// Seeds sector-specific compliance rules based on Mauritius Remuneration Orders 2025
    /// </summary>
    public static void SeedSectorComplianceRules(MasterDbContext context)
    {
        if (context.SectorComplianceRules.Any())
        {
            Console.WriteLine("[INFO] Sector compliance rules already seeded. Skipping...");
            return;
        }

        Console.WriteLine("[INFO] Seeding sector compliance rules...");

        var rules = new List<SectorComplianceRule>();
        var ruleId = 1;

        // Get sector IDs (after seeding sectors)
        var cateringHotelLarge = context.IndustrySectors.First(s => s.SectorCode == "CAT-HOTEL-LARGE").Id;
        var bpoSector = context.IndustrySectors.First(s => s.SectorCode == "BPO").Id;
        var securitySector = context.IndustrySectors.First(s => s.SectorCode == "SECURITY").Id;
        var bankingSector = context.IndustrySectors.First(s => s.SectorCode == "FINANCE-BANKING").Id;
        var constructionSector = context.IndustrySectors.First(s => s.SectorCode == "CONST").Id;
        var retailSupermarket = context.IndustrySectors.First(s => s.SectorCode == "RETAIL-SUPERMARKET").Id;

        // ===== CATERING & TOURISM - HOTEL (LARGE) RULES =====

        // 1. OVERTIME Rules
        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = cateringHotelLarge,
            RuleCategory = "OVERTIME",
            RuleName = "Catering & Tourism - Overtime Rates",
            RuleConfig = @"{
                ""weekday_overtime_rate"": 1.5,
                ""weekend_overtime_rate"": 2.0,
                ""sunday_rate"": 2.0,
                ""public_holiday_normal_hours_rate"": 2.0,
                ""public_holiday_after_hours_rate"": 3.0,
                ""cyclone_warning_class_3_rate"": 3.0,
                ""night_shift_rate"": 1.25,
                ""max_overtime_hours_per_day"": 4,
                ""max_overtime_hours_per_week"": 20,
                ""meal_allowance_after_hours"": 2,
                ""meal_allowance_amount_mur"": 85,
                ""time_off_in_lieu_allowed"": true
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 185 of 2023 - Catering & Tourism Remuneration Order",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // 2. MINIMUM_WAGE Rules
        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = cateringHotelLarge,
            RuleCategory = "MINIMUM_WAGE",
            RuleName = "Catering & Tourism - Minimum Wage 2025",
            RuleConfig = @"{
                ""monthly_minimum_wage_mur"": 17110,
                ""salary_compensation_mur"": 610,
                ""effective_date"": ""2025-01-01"",
                ""applies_to_basic_salary_up_to_mur"": 50000,
                ""currency"": ""MUR"",
                ""notes"": ""Minimum wage increased Jan 2025 from MUR 16,500 to MUR 17,110. Salary compensation increased from MUR 500 to MUR 610.""
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 185 of 2023 + National Minimum Wage Regulations 2025",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // 3. WORKING_HOURS Rules
        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = cateringHotelLarge,
            RuleCategory = "WORKING_HOURS",
            RuleName = "Catering & Tourism - Working Hours",
            RuleConfig = @"{
                ""standard_weekly_hours"": 45,
                ""standard_daily_hours"": 9,
                ""working_pattern_options"": [
                    ""9 hours x 5 days per week"",
                    ""8 hours x 5 days + 5 hours x 1 day""
                ],
                ""mandatory_lunch_break_minutes"": 60,
                ""daily_max_hours"": 12,
                ""unsocial_hours_start"": ""22:00"",
                ""unsocial_hours_end"": ""06:00"",
                ""shift_patterns_allowed"": true,
                ""rotational_shifts"": true
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 185 of 2023 - Catering & Tourism Remuneration Order",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // 4. ALLOWANCES Rules
        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = cateringHotelLarge,
            RuleCategory = "ALLOWANCES",
            RuleName = "Catering & Tourism - Allowances",
            RuleConfig = @"{
                ""meal_allowance_per_shift_mur"": 85,
                ""transport_allowance_per_month_mur"": 0,
                ""uniform_allowance_per_year_mur"": 500,
                ""housing_allowance_applicable"": false,
                ""tips_pooling_allowed"": true,
                ""service_charge_distribution"": ""As per hotel policy""
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 185 of 2023",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // 5. LEAVE Rules
        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = cateringHotelLarge,
            RuleCategory = "LEAVE",
            RuleName = "Catering & Tourism - Leave Entitlements",
            RuleConfig = @"{
                ""annual_leave_days"": 22,
                ""sick_leave_days"": 15,
                ""casual_leave_days"": 5,
                ""maternity_leave_weeks"": 14,
                ""paternity_leave_days"": 5,
                ""leave_calculation_basis"": ""working_days"",
                ""annual_leave_carry_forward_max_days"": 5,
                ""encashment_allowed_on_resignation"": true
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "Workers' Rights Act 2019 + GN No. 185 of 2023",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // 6. GRATUITY Rules
        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = cateringHotelLarge,
            RuleCategory = "GRATUITY",
            RuleName = "Catering & Tourism - Gratuity Calculation",
            RuleConfig = @"{
                ""formula"": ""15 days per year of service"",
                ""calculation_basis"": ""basic_salary"",
                ""minimum_service_months"": 12,
                ""max_gratuity_amount_mur"": null,
                ""pro_rated_for_partial_year"": true,
                ""notes"": ""Gratuity = (Basic Salary / 22) * 15 * Years of Service""
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "Workers' Rights Act 2019 Section 111",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== BPO SECTOR RULES =====

        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = bpoSector,
            RuleCategory = "OVERTIME",
            RuleName = "BPO - Overtime Rates",
            RuleConfig = @"{
                ""weekday_overtime_rate"": 1.5,
                ""weekend_overtime_rate"": 2.0,
                ""sunday_rate"": 2.0,
                ""public_holiday_normal_hours_rate"": 2.0,
                ""public_holiday_after_hours_rate"": 3.0,
                ""night_shift_allowance_percentage"": 10,
                ""max_overtime_hours_per_day"": 4,
                ""max_overtime_hours_per_week"": 20,
                ""time_off_in_lieu_allowed"": true
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 201 of 2023 - BPO Remuneration Order",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = bpoSector,
            RuleCategory = "MINIMUM_WAGE",
            RuleName = "BPO - Minimum Wage 2025",
            RuleConfig = @"{
                ""monthly_minimum_wage_mur"": 17110,
                ""salary_compensation_mur"": 610,
                ""effective_date"": ""2025-01-01"",
                ""applies_to_basic_salary_up_to_mur"": 50000,
                ""currency"": ""MUR""
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 201 of 2023 + National Minimum Wage Regulations 2025",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = bpoSector,
            RuleCategory = "WORKING_HOURS",
            RuleName = "BPO - Working Hours",
            RuleConfig = @"{
                ""standard_weekly_hours"": 45,
                ""standard_daily_hours"": 9,
                ""working_pattern_options"": [
                    ""9 hours x 5 days per week"",
                    ""8 hours x 5 days + 5 hours x 1 day""
                ],
                ""mandatory_break_minutes"": 60,
                ""daily_max_hours"": 12,
                ""shift_patterns_allowed"": true,
                ""rotational_shifts"": true,
                ""night_shifts_allowed"": true
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 201 of 2023",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== SECURITY SECTOR RULES =====

        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = securitySector,
            RuleCategory = "OVERTIME",
            RuleName = "Security Services - Overtime Rates",
            RuleConfig = @"{
                ""weekday_overtime_rate"": 1.25,
                ""weekend_overtime_rate"": 1.5,
                ""sunday_rate"": 2.0,
                ""public_holiday_normal_hours_rate"": 2.0,
                ""public_holiday_after_hours_rate"": 2.5,
                ""night_shift_rate"": 1.1,
                ""max_overtime_hours_per_day"": 6,
                ""max_overtime_hours_per_week"": 24,
                ""time_off_in_lieu_allowed"": false
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 178 of 2023 - Security Services Remuneration Order",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = securitySector,
            RuleCategory = "WORKING_HOURS",
            RuleName = "Security Services - Working Hours",
            RuleConfig = @"{
                ""standard_weekly_hours"": 48,
                ""standard_daily_hours"": 8,
                ""working_pattern_options"": [
                    ""8 hours x 6 days per week"",
                    ""12-hour shifts (rotating)""
                ],
                ""mandatory_break_minutes"": 60,
                ""daily_max_hours"": 14,
                ""shift_patterns_allowed"": true,
                ""rotational_shifts"": true,
                ""continuous_duty_max_hours"": 12
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 178 of 2023",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== BANKING SECTOR RULES =====

        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = bankingSector,
            RuleCategory = "WORKING_HOURS",
            RuleName = "Banking - Working Hours",
            RuleConfig = @"{
                ""standard_weekly_hours"": 40,
                ""standard_daily_hours"": 8,
                ""working_pattern_options"": [
                    ""8 hours x 5 days per week (Monday-Friday)""
                ],
                ""mandatory_break_minutes"": 60,
                ""daily_max_hours"": 10,
                ""shift_patterns_allowed"": false,
                ""weekend_work_exceptional_only"": true
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 189 of 2023 - Financial Services Remuneration Order",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = bankingSector,
            RuleCategory = "OVERTIME",
            RuleName = "Banking - Overtime Rates",
            RuleConfig = @"{
                ""weekday_overtime_rate"": 1.5,
                ""weekend_overtime_rate"": 2.5,
                ""sunday_rate"": 2.5,
                ""public_holiday_rate"": 3.0,
                ""max_overtime_hours_per_week"": 10,
                ""time_off_in_lieu_preferred"": true,
                ""notes"": ""Banking sector typically minimizes overtime due to strict working hours""
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 189 of 2023",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== CONSTRUCTION SECTOR RULES =====

        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = constructionSector,
            RuleCategory = "OVERTIME",
            RuleName = "Construction - Overtime Rates",
            RuleConfig = @"{
                ""weekday_overtime_rate"": 1.5,
                ""weekend_overtime_rate"": 2.0,
                ""sunday_rate"": 2.0,
                ""public_holiday_rate"": 2.5,
                ""night_shift_rate"": 1.25,
                ""max_overtime_hours_per_day"": 4,
                ""max_overtime_hours_per_week"": 20
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 162 of 2022 - Construction Remuneration Order",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = constructionSector,
            RuleCategory = "ALLOWANCES",
            RuleName = "Construction - Allowances",
            RuleConfig = @"{
                ""transport_allowance_per_day_mur"": 50,
                ""meal_allowance_per_day_mur"": 85,
                ""tool_allowance_per_month_mur"": 200,
                ""height_work_allowance_per_day_mur"": 100,
                ""hazard_allowance_applicable"": true,
                ""safety_equipment_provided_by_employer"": true
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 162 of 2022",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // ===== RETAIL SUPERMARKET RULES =====

        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = retailSupermarket,
            RuleCategory = "WORKING_HOURS",
            RuleName = "Retail - Working Hours",
            RuleConfig = @"{
                ""standard_weekly_hours"": 45,
                ""standard_daily_hours"": 9,
                ""working_pattern_options"": [
                    ""9 hours x 5 days per week"",
                    ""8 hours x 5 days + 5 hours x 1 day""
                ],
                ""mandatory_break_minutes"": 60,
                ""daily_max_hours"": 12,
                ""shift_patterns_allowed"": true,
                ""sunday_trading_hours_restricted"": true,
                ""public_holiday_trading_requires_approval"": true
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 183 of 2023 - Retail & Distributive Trade Remuneration Order",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        rules.Add(new SectorComplianceRule
        {
            Id = ruleId++,
            SectorId = retailSupermarket,
            RuleCategory = "OVERTIME",
            RuleName = "Retail - Overtime Rates",
            RuleConfig = @"{
                ""weekday_overtime_rate"": 1.5,
                ""weekend_overtime_rate"": 2.0,
                ""sunday_rate"": 2.5,
                ""public_holiday_rate"": 3.0,
                ""max_overtime_hours_per_day"": 4,
                ""max_overtime_hours_per_week"": 20,
                ""time_off_in_lieu_allowed"": true
            }",
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            LegalReference = "GN No. 183 of 2023",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        context.SectorComplianceRules.AddRange(rules);
        context.SaveChanges();

        Console.WriteLine($"[SUCCESS] Seeded {rules.Count} sector compliance rules");
    }
}
