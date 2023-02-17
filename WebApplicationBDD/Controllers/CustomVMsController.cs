using Microsoft.AspNetCore.Mvc;
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

namespace CloudplayWebApp.Controllers
{
    /// <summary>
    /// Virtual Machine Controller
    /// </summary>
    [TypeFilter(typeof(AuthorizationFilter))]
    public class CustomVMsController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Resource Groupe name
        /// </summary>
        public const string RG_NAME = "rg-gaming-667";

        /// <summary>
        /// Virtual machine name
        /// </summary>
        public const string VM_NAME = "VM03";

        /// <summary>
        /// NIC name
        /// </summary>
        public const string NIC_NAME = "MyNic";

        /// <summary>
        /// Virtual Network name
        /// </summary>
        public const string VN_NAME = "testVN";

        /// <summary>
        /// IP adress name
        /// </summary>
        public const string IP_NAME = "IPxd";

        public CustomVMsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Check if the VM is already created 
        /// </summary>
        /// <returns>true if a VM already exists, false otherwise </returns>
        public static async Task<bool> IsThereAVMAsync()
        {
            ArmClient armClient = new(new ClientSecretCredential("b7b023b8-7c32-4c02-92a6-c8cdaa1d189c", "180d646d-d208-450a-bd63-397d3a7be25d", "Kct8Q~WpQGqBJCJ6YtQeHB_6QlLfmUm.4vsrQbrX"));
            ResourceGroupResource resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(RG_NAME);

            VirtualMachineResource vm = await resourceGroup.GetVirtualMachines().GetAsync(VM_NAME);

            return vm != null;
        }

        // GET: CustomVMs
        public async Task<IActionResult> Index()
        {
            return _context.CustomVMs != null ?
                        View(await _context.CustomVMs.ToListAsync()) :
                        Problem("Entity set 'ApplicationDbContext.CustomVMs'  is null.");
        }

        /// <summary>
        /// Create an IP adress
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <returns>the IP adress</returns>
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

                ArmClient armClient = new(new ClientSecretCredential("b7b023b8-7c32-4c02-92a6-c8cdaa1d189c", "180d646d-d208-450a-bd63-397d3a7be25d", "Kct8Q~WpQGqBJCJ6YtQeHB_6QlLfmUm.4vsrQbrX"));
                ResourceGroupResource resourceGroup = armClient.GetDefaultSubscription().GetResourceGroup(RG_NAME);

                //Getting resources to delete
                VirtualMachineResource vm = await resourceGroup.GetVirtualMachines().GetAsync(VM_NAME);
                NetworkInterfaceResource nic = await resourceGroup.GetNetworkInterfaces().GetAsync(NIC_NAME);
                VirtualNetworkResource vnet = await resourceGroup.GetVirtualNetworks().GetAsync(VN_NAME);
                PublicIPAddressResource publicIp = await resourceGroup.GetPublicIPAddresses().GetAsync(IP_NAME);

                //Deleting
                await vm.DeleteAsync(WaitUntil.Completed, forceDeletion: true);
                await nic.DeleteAsync(WaitUntil.Completed);
                await vnet.DeleteAsync(WaitUntil.Completed);
                await publicIp.DeleteAsync(WaitUntil.Completed);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomVMExists(string id)
        {
            return (_context.CustomVMs?.Any(e => e.Name == id)).GetValueOrDefault();
        }
        /// <summary>
        /// Check if the VM is running or starting
        /// </summary>
        /// <param name="name">name of the VM</param>
        /// <returns>true if its running, false otherwise</returns>
        public static async Task<bool> VMstatus(string name)
        {
            bool resultat = false;
            ArmClient armClient = new(new ClientSecretCredential("b7b023b8-7c32-4c02-92a6-c8cdaa1d189c", "180d646d-d208-450a-bd63-397d3a7be25d", "Kct8Q~WpQGqBJCJ6YtQeHB_6QlLfmUm.4vsrQbrX"));
            SubscriptionResource subscription = armClient.GetDefaultSubscription();
            ResourceGroupData resourceGroupData = new(AzureLocation.NorthEurope);
            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
            await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, RG_NAME, resourceGroupData);
            ResourceGroupResource resourceGroupResource = armClient.GetDefaultSubscription().GetResourceGroup(RG_NAME);


            VirtualMachineCollection vms = resourceGroupResource.GetVirtualMachines();

            foreach (VirtualMachineResource vm in vms)
            {
                if (name.Equals(vm.Id.Name))
                {

                    resultat = vm.InstanceView(CancellationToken.None).Value.Statuses[1].Code.Equals("PowerState/stopped");
                }
            }
            return resultat;
        }

        /// <summary>
        /// Runs or stops the VM depending of its state
        /// </summary>
        /// <param name="nom">VM name</param>
        /// <returns></returns>
        public async Task<IActionResult> RunOrStop(string nom)
        {
            //Connect to Azure with Visual Studio credentials
            ArmClient client = new(new ClientSecretCredential("b7b023b8-7c32-4c02-92a6-c8cdaa1d189c", "180d646d-d208-450a-bd63-397d3a7be25d", "Kct8Q~WpQGqBJCJ6YtQeHB_6QlLfmUm.4vsrQbrX"));
            //Gets subscription
            SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
            //Gets all resource groups
            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();

            //Create resource group
            ResourceGroupData resourceGroupData = new(AzureLocation.NorthEurope);
            
            await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, RG_NAME, resourceGroupData);

            //Gets target resource group
            ResourceGroupResource resourceGroup = await resourceGroups.GetAsync(RG_NAME);

            VirtualMachineResource virtualMachine = await resourceGroup.GetVirtualMachineAsync(VM_NAME);

            VirtualMachineCollection vms = resourceGroup.GetVirtualMachines();

            foreach (VirtualMachineResource vm in vms)
            {
                if (nom.Equals(vm.Id.Name))
                {
                    if ((await VMstatus(nom)))
                    {
                        await vm.PowerOnAsync(WaitUntil.Completed);
                    }
                    else
                    {
                        await vm.PowerOffAsync(WaitUntil.Completed);
                    }
                }
            }
            return RedirectToAction(nameof(Index));
        }
        /// <summary>
        /// Creates a Virtual Machine in Azure
        /// </summary>
        /// <param name="customVM">the VM to be created</param>
        /// <returns></returns>
        private async Task CreateVirtualMachineAsync(CustomVM customVM)
        {

            //Connect to Azure with Visual Studio credentials
            ArmClient client = new(new ClientSecretCredential("b7b023b8-7c32-4c02-92a6-c8cdaa1d189c", "180d646d-d208-450a-bd63-397d3a7be25d", "Kct8Q~WpQGqBJCJ6YtQeHB_6QlLfmUm.4vsrQbrX"));

            //Gets subscription
            SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
            //Gets all resource groups
            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();

            //Create resource group
            ResourceGroupData resourceGroupData = new(AzureLocation.NorthEurope);
            await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, RG_NAME, resourceGroupData);

            customVM.Name = VM_NAME;
            customVM.IP = IP_NAME;

            //Gets target resource group
            ResourceGroupResource resourceGroup = await resourceGroups.GetAsync(RG_NAME);

            VirtualMachineCollection vms = resourceGroup.GetVirtualMachines();
            NetworkInterfaceCollection nics = resourceGroup.GetNetworkInterfaces();
            VirtualNetworkCollection vns = resourceGroup.GetVirtualNetworks();

            PublicIPAddressResource ipResource = InitIp(resourceGroup);

            VirtualNetworkResource vnetResrouce = vns.CreateOrUpdate(
                WaitUntil.Completed,
                VN_NAME,
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
                NIC_NAME,
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
                        OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage) { DeleteOption = DiskDeleteOptionType.Delete, },
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
