namespace ZoneTree.FullTextSearch.UnitTests.sampleData;

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public decimal Price { get; set; }
    public string Brand { get; set; }
    public double Rating { get; set; }
    public Facets Facets { get; set; }
    public string Description { get; set; }

    public override string ToString()
    {
        return $"Product: {Id} {Name} {Category} {Price} {Brand} {Rating} {Description}";
    }
}

public class Facets
{
    public string Color { get; set; }
    public string Connectivity { get; set; }
    public string BatteryLife { get; set; }
    public string[] Features { get; set; }
    public string ScreenSize { get; set; }
    public string Resolution { get; set; }
    public string[] SmartFeatures { get; set; }
    public string[] Ports { get; set; }
    public string Capacity { get; set; }
    public string EnergyEfficiency { get; set; }
    public string Processor { get; set; }
    public string Ram { get; set; }
    public string Storage { get; set; }
    public string GraphicsCard { get; set; }
    public string Pressure { get; set; }
}

public static class ProductList
{
    public static Product[] Products =
    [
        new Product
        {
            Id = 1,
            Name = "Wireless Noise Cancelling Headphones",
            Category = "Electronics",
            Price = 299.99m,
            Brand = "AudioPro",
            Rating = 4.8,
            Facets = new Facets
            {
                Color = "Black",
                Connectivity = "Bluetooth",
                BatteryLife = "30 hours",
                Features = ["Noise Cancelling", "Wireless", "Over-Ear"]
            },
            Description = "Experience immersive sound with AudioPro's Wireless Noise Cancelling Headphones. With up to 30 hours of battery life and advanced noise-cancelling technology, these headphones are perfect for uninterrupted music on the go."
        },
        new Product
        {
            Id = 2,
            Name = "Smart LED TV 55\" 4K UHD",
            Category = "Home Appliances",
            Price = 599.99m,
            Brand = "VisionTech",
            Rating = 4.5,
            Facets = new Facets
            {
                ScreenSize = "55 inches",
                Resolution = "4K UHD",
                SmartFeatures = ["Voice Control", "Streaming Apps", "Wi-Fi"],
                Ports = ["HDMI", "USB", "Ethernet"]
            },
            Description = "Upgrade your home entertainment with VisionTech's 55\" 4K UHD Smart LED TV. Enjoy stunning visuals and smart features like voice control and streaming apps, all in one sleek package."
        },
        new Product
        {
            Id = 3,
            Name = "Stainless Steel Refrigerator",
            Category = "Home Appliances",
            Price = 899.99m,
            Brand = "CoolHome",
            Rating = 4.7,
            Facets = new Facets
            {
                Capacity = "25 cu. ft.",
                EnergyEfficiency = "A+",
                Features = ["Water Dispenser", "Ice Maker", "LED Lighting"],
                Color = "Stainless Steel"
            },
            Description = "Keep your food fresh and organized with CoolHome's 25 cu. ft. Stainless Steel Refrigerator. With a built-in water dispenser and ice maker, this fridge combines style and functionality for any modern kitchen."
        },
        new Product
        {
            Id = 4,
            Name = "Gaming Laptop 15\"",
            Category = "Computers",
            Price = 1299.99m,
            Brand = "GamerX",
            Rating = 4.9,
            Facets = new Facets
            {
                Processor = "Intel i7",
                Ram = "16GB",
                Storage = "512GB SSD",
                GraphicsCard = "NVIDIA GTX 1660 Ti",
                ScreenSize = "15 inches"
            },
            Description = "Take your gaming to the next level with the GamerX 15\" Gaming Laptop. Equipped with an Intel i7 processor and NVIDIA GTX 1660 Ti graphics card, this laptop delivers powerful performance for the most demanding games."
        },
        new Product
        {
            Id = 5,
            Name = "Espresso Coffee Machine",
            Category = "Kitchen Appliances",
            Price = 199.99m,
            Brand = "BrewMaster",
            Rating = 4.6,
            Facets = new Facets
            {
                Capacity = "1.5 liters",
                Pressure = "15 bar",
                Features = ["Milk Frother", "Programmable Settings", "Removable Drip Tray"],
                Color = "Red"
            },
            Description = "Brew the perfect cup of espresso with BrewMaster's Espresso Coffee Machine. Featuring a 15 bar pump and programmable settings, this machine makes it easy to enjoy café-quality coffee at home."
        },
        new Product
        {
            Id = 6,
            Name = "Smartphone 128GB",
            Category = "Electronics",
            Price = 799.99m,
            Brand = "TechWave",
            Rating = 4.7,
            Facets = new Facets
            {
                Color = "Midnight Blue",
                Connectivity = "5G",
                BatteryLife = "24 hours",
                Features = ["Face Recognition", "Wireless Charging", "Triple Camera"]
            },
            Description = "TechWave's 128GB Smartphone offers cutting-edge technology with 5G connectivity, a powerful battery, and a triple camera system for stunning photos and videos."
        },
        new Product
        {
            Id = 7,
            Name = "Electric Mountain Bike",
            Category = "Outdoor",
            Price = 1299.99m,
            Brand = "RideXtreme",
            Rating = 4.8,
            Facets = new Facets
            {
                Color = "Matte Black",
                BatteryLife = "60 miles",
                Features = ["Pedal Assist", "Hydraulic Brakes", "Shock Absorbers"]
            },
            Description = "Explore the outdoors with RideXtreme's Electric Mountain Bike, featuring a powerful motor, long battery life, and advanced suspension for all-terrain adventures."
        },
        new Product
        {
            Id = 8,
            Name = "4K Action Camera",
            Category = "Cameras",
            Price = 249.99m,
            Brand = "AdventureCam",
            Rating = 4.4,
            Facets = new Facets
            {
                Color = "Silver",
                Connectivity = "Wi-Fi",
                Features = ["Waterproof", "Image Stabilization", "4K Video"]
            },
            Description = "Capture every moment with the AdventureCam 4K Action Camera. Its waterproof design and image stabilization make it perfect for any adventure."
        },
        new Product
        {
            Id = 9,
            Name = "Digital Air Fryer",
            Category = "Kitchen Appliances",
            Price = 99.99m,
            Brand = "HealthyCook",
            Rating = 4.5,
            Facets = new Facets
            {
                Capacity = "5 liters",
                Features = ["Touch Screen", "Adjustable Temperature", "Non-stick Basket"],
                Color = "White"
            },
            Description = "HealthyCook's Digital Air Fryer makes healthy cooking easy. With a large capacity and touch screen controls, you can fry, bake, and grill your favorite meals with little to no oil."
        },
        new Product
        {
            Id = 10,
            Name = "Portable Bluetooth Speaker",
            Category = "Electronics",
            Price = 49.99m,
            Brand = "SoundBlitz",
            Rating = 4.3,
            Facets = new Facets
            {
                Color = "Camo Green",
                Connectivity = "Bluetooth 5.0",
                BatteryLife = "12 hours",
                Features = ["Waterproof", "Built-in Microphone", "Stereo Pairing"]
            },
            Description = "Enjoy high-quality sound on the go with the SoundBlitz Portable Bluetooth Speaker. Its waterproof design and long battery life make it perfect for any adventure."
        }];
}
