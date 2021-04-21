# Download and run a release
1. Download a release from https://github.com/arielflashner/ShellyDiscovery/releases
2. Extract zip file
3. Run ShellyDiscovery.exe

The application should try to fetch the shelly device and relay names and list them along with their IP address:
![image](https://user-images.githubusercontent.com/3799599/115608480-fddd7f80-a2ee-11eb-8c2d-9aa54678c7fb.png)

In case you chose to to show the data group by a certain property, a summary will be displayed once the scan is finished  
Nested attribute names can be used like "device.type"

# Run from source
````
git clone https://github.com/arielflashner/ShellyDiscovery.git
cd ShellyDiscovery
dotnet run
````
