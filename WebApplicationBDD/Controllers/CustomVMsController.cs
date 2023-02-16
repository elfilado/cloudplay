using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CloudplayWebApp.Data;
using CloudplayWebApp.Models;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Azure.Management.Compute.Models;

namespace CloudplayWebApp.Controllers
{
    public class CustomVMsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomVMsController(ApplicationDbContext context)
        {
            _context = context;
        }
        private string GetUserName()
        {
            string user = string.Empty;
            if (HttpContext.User.Identity != null &&
                HttpContext.User.Identity.Name != null)
            {
                user = HttpContext.User.Identity.Name;
                user = user.Split("@")[0].Replace(".", "");
            }
            return user;
        }

        private static async Task StopVMAsync()
        {
            //Connect to Azure with Visual Studio credentials
            ArmClient client = new(new DefaultAzureCredential());
            //Gets subscription
            SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
            //Gets all resource groups
            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();

            //Create resource group
            ResourceGroupData resourceGroupData = new(AzureLocation.NorthEurope);
            await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, "rg-gaming-011", resourceGroupData);

            //Gets target resource group
            ResourceGroupResource resourceGroup = await resourceGroups.GetAsync("rg-gaming-011");

            VirtualMachineResource virtualMachine = await resourceGroup.GetVirtualMachineAsync("MM01");         
            await virtualMachine.PowerOffAsync(WaitUntil.Completed);          
        }

        // GET: CustomVMs
        public async Task<IActionResult> Index()
        {
            return _context.CustomVMs != null ?
                        View(await _context.CustomVMs.ToListAsync()) :
                        Problem("Entity set 'ApplicationDbContext.CustomVMs'  is null.");
        }
        private PublicIPAddressResource InitIp(ResourceGroupResource resourceGroup)
        {
            var publicIps = resourceGroup.GetPublicIPAddresses();
            var ipResource = publicIps.CreateOrUpdate(
                WaitUntil.Completed,
                "IPxd",
                new PublicIPAddressData()
                {
                    PublicIPAddressVersion = NetworkIPVersion.IPv4,
                    PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    Location = AzureLocation.NorthEurope
                }).Value;

            return ipResource;
        }

        // GET: CustomVMs/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.CustomVMs == null)
            {
                return NotFound();
            }

            var customVM = await _context.CustomVMs
                .FirstOrDefaultAsync(m => m.Name == id);
            if (customVM == null)
            {
                return NotFound();
            }

            return View(customVM);
        }

        //public async Task<IActionResult> Stop(string id)
        //{
        //    if (id == null || _context.CustomVMs == null)
        //    {
        //        return NotFound();
        //    }

        //    var customVM = await _context.CustomVMs
        //        .FirstOrDefaultAsync(m => m.Name == id);
        //    if (customVM == null)
        //    {
        //        return NotFound();
        //    }
        //    await customVM.
        //}

        public static void StartAzureVm(string? name)
        {
            var armClient = new ArmClient(new DefaultAzureCredential());

            var subscription = armClient.GetDefaultSubscriptionAsync().Result;

            var resourceGroups = subscription.GetResourceGroups();
            ResourceGroupResource resourceGroup = resourceGroups.GetAsync("rg-gaming-010").Result;

            var virtualMachines = resourceGroup.GetVirtualMachines();
            var virtualMachine = virtualMachines.GetAsync(name).Result.Value;
            virtualMachine.PowerOn(WaitUntil.Completed);
        }
        [HttpPost, ActionName("Run")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunAsync(string id)
        {
            if (id == null || _context.CustomVMs == null)
            {
                return NotFound();
            }

            var customVM = await _context.CustomVMs
                .FirstOrDefaultAsync(m => m.Name == id);
            if (customVM == null)
            {
                return NotFound();
            }
            StartAzureVm(id);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: CustomVMs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CustomVMs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Login,Password")] CustomVM customVM)
        {
            ModelState.Remove(nameof(CustomVM.IP));
            ModelState.Remove(nameof(CustomVM.Name));
            if (ModelState.IsValid)
            {
                await CreateVirtualMachineAsync(customVM);
                _context.Add(customVM);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }    
            return View(customVM);
        }

        // GET: CustomVMs/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.CustomVMs == null)
            {
                return NotFound();
            }

            var customVM = await _context.CustomVMs.FindAsync(id);
            if (customVM == null)
            {
                return NotFound();
            }
            return View(customVM);
        }

        // POST: CustomVMs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Name,IP,Login,Password")] CustomVM customVM)
        {
            if (id != customVM.Name)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customVM);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomVMExists(customVM.Name))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customVM);
        }

        // GET: CustomVMs/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.CustomVMs == null)
            {
                return NotFound();
            }

            var customVM = await _context.CustomVMs
                .FirstOrDefaultAsync(m => m.Name == id);
            if (customVM == null)
            {
                return NotFound();
            }

            return View(customVM);
        }

        // POST: CustomVMs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.CustomVMs == null)
            {
                return Problem("Entity set 'ApplicationDbContext.CustomVMs'  is null.");
            }
            var customVM = await _context.CustomVMs.FindAsync(id);
            if (customVM != null)
            {
                _context.CustomVMs.Remove(customVM);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomVMExists(string id)
        {
            return (_context.CustomVMs?.Any(e => e.Name == id)).GetValueOrDefault();
        }

        private async Task CreateVirtualMachineAsync(CustomVM customVM)
        {

            //Connect to Azure with Visual Studio credentials
            ArmClient client = new(new DefaultAzureCredential());
            //Gets subscription
            SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
            //Gets all resource groups
            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();

            //Create resource group
            ResourceGroupData resourceGroupData = new(AzureLocation.NorthEurope);
            await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, "rg-gaming-010", resourceGroupData);

            customVM.Name = "VM01";
            customVM.IP = "MyIP";

            //Gets target resource group
            ResourceGroupResource resourceGroup = await resourceGroups.GetAsync("rg-gaming-010");

            VirtualMachineCollection vms = resourceGroup.GetVirtualMachines();
            NetworkInterfaceCollection nics = resourceGroup.GetNetworkInterfaces();
            VirtualNetworkCollection vns = resourceGroup.GetVirtualNetworks();

            PublicIPAddressResource ipResource = InitIp(resourceGroup);

            VirtualNetworkResource vnetResrouce = vns.CreateOrUpdate(
                WaitUntil.Completed,
                "testVN",
                new VirtualNetworkData()
                {
                    Location = AzureLocation.NorthEurope,
                    Subnets =
                    {
                        new SubnetData()
                        {
                            Name = "testSubNet",
                            AddressPrefix = "10.0.0.0/24"
                        }
                    },
                    AddressPrefixes =
                    {
                         "10.0.0.0/16"
                    },
                }).Value;

            NetworkInterfaceResource nicResource = nics.CreateOrUpdate(
                WaitUntil.Completed,
                "MyNic",
                new NetworkInterfaceData()
                {
                    Location = AzureLocation.NorthEurope,
                    IPConfigurations =
                    {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new SubnetData() { Id = vnetResrouce?.Data.Subnets.First().Id },
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            PublicIPAddress = new PublicIPAddressData() { Id = ipResource?.Data.Id }
                        }
                    }
                }).Value;

            VirtualMachineResource vmResource = vms.CreateOrUpdate(
                WaitUntil.Completed,
                customVM.Name,
                new VirtualMachineData(AzureLocation.NorthEurope)
                {
                    HardwareProfile = new VirtualMachineHardwareProfile()
                    {
                        VmSize = VirtualMachineSizeType.StandardB2S
                    },
                    OSProfile = new VirtualMachineOSProfile()
                    {
                        ComputerName = customVM.Name,
                        AdminUsername = customVM.Login,
                        AdminPassword = customVM.Password,
                        WindowsConfiguration = new Azure.ResourceManager.Compute.Models.WindowsConfiguration()
                        {
                            ProvisionVmAgent = true
                        }
                    },
                    StorageProfile = new VirtualMachineStorageProfile()
                    {
                        OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage),
                        ImageReference = new Azure.ResourceManager.Compute.Models.ImageReference()
                        {
                            Offer = "Windows-10",
                            Publisher = "MicrosoftWindowsDesktop",
                            Sku = "19h2-ent",
                            Version = "latest"
                        }
                    },
                    NetworkProfile = new VirtualMachineNetworkProfile()
                    {
                        NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference()
                            {
                                Id = nicResource.Id
                            }
                        }
                    },
                }).Value;
            customVM.IP = InitIp(resourceGroup).Data.IPAddress;
        }
    }
}
