# Spirit Sling Tabletop
![Banner](./Documentation/Images/SpiritSling_Marketing_SmallLandscape.png)

Spirit Sling is a social mixed reality (MR) app created to show developers how to build exciting tabletop games that give users a reason to be together in MR. With Meta's new and improved Avatars, and the power of mixed reality, users can now place a fun tabletop experience on a surface and invite a friend into their space to play with them.

The majority of Spirit Sling is licensed under [MIT LICENSE](./LICENSE), however files from [Text Mesh Pro](http://www.unity3d.com/legal/licenses/Unity_Companion_License), and [Photon SDK](./Assets/Photon/LICENSE), are licensed under their respective licensing terms.

See the [CONTRIBUTING](./CONTRIBUTING.md) file for how to help out.

This project was built using the [Unity engine](https://unity.com/) with [Photon Fusion](https://doc.photonengine.com/fusion/current/getting-started/fusion-intro).

Test the game on [Meta Quest Store - Spirit Sling Tabletop](https://www.meta.com/en-gb/experiences/spirit-sling-tabletop/26801347429479910/).

## How to run the project in Unity
1. [Configure the project](./Documentation/ProjectConfiguration.md) with Meta Quest and Photon
2. Make sure you're using 2022.3.30.
3. Make sure you're using one of these devices: Meta Quest 3S, Meta Quest 3, Meta Quest Pro.
4. Locate the 'Assets/GameSettings.asset' file and populate all the empty fields with your own data.  
4.1. 'Application Identifier' is the unique string that identifies your app on Meta Quest Store.  
4.2. 'Meta Quest App ID' is the ID of your app on Meta Quest Store.  
4.3. Optional: populate Android Keystore name and password. Make sure not to store the 'Assets/GameSettings.asset' file in VCS in this case.  
4.4. 'Photon App Id Fusion / Voice': unique ids obtained in step 1 in the 'Photon Configuration' section.
![Game Settings](./Documentation/Images/GameSettings.png)

## Project Structure
The project is organically structured to distinguish the main components of the MR experience's logic. Main Spirit Sling components:
- **ConnectionManager** handles the Photon Fusion connection workflows for single and multiplayer sessions. The PhotonConnector logic showcases how a shared multiplayer session is handled via Photon Fusion, how the creation of shared rooms and lobbies work, and how the connection states can be handled accordingly.
- **GameVolumeSpawner** handles logic for placing the game board into physical environment. It also ensures that the game board is accessible by hands so the user can readjust the initial placement for a more comfortable experience.
- **TabletopGameStateMachine** controls the flow of the game between different gameplay states.
- Please visit the [Meta Horizon Documentation](https://developers.meta.com/horizon/documentation/unity/spirit-sling/#intractable-virtual-objects-using-isdk-and-physics-to-enhance-gameplay) page for more details on how the game uses Mixed Reality Utility Kit (MRUK), Platform SDK and Meta Interaction SDK (ISDK).

# Dependencies
This project makes use of the following plugins and software:
- [Mixed Reality Utility Kit v74.0.1](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-overview/)
- [Meta XR Core SDK v74.0.1](https://developers.meta.com/horizon/downloads/package/meta-xr-core-sdk)
- [Meta Interaction SDK v74.0.1](https://developers.meta.com/horizon/documentation/unity/unity-isdk-interaction-sdk-overview/)
- [Meta Avatars SDK v31.0.0](https://developers.meta.com/horizon/documentation/unity/meta-avatars-overview/)