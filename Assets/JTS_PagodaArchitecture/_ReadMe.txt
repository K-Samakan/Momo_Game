---------------------------------------------------
(JTS) PAGODA ARCHITECTURE

Copyright ©2018 Tanuki Digital
Version 1.0
---------------------------------------------------


----------------------------
THANK YOU FOR YOUR PURCHASE!
----------------------------
Thank you for buying PAGODA ARCHITECTURE and supporting Tanuki Digital!
It's people like you that allow us to build and improve our software! 
If you have any questions, comments, or requests for new features
please visit the Tanuki Digital Forums and post your feedback:

http://tanukidigital.com/forum/
or email us directly at: konnichiwa@tanukidigital.com

If you find this product useful, please consider leaving feedback and a review at the Asset Store Page.



----------------------
REGISTER YOUR PURCHASE
----------------------
Did you purchase on the Unity Asset Store?
Registering at Tanuki Digital.com gives you immediate access to new downloads, updates, as well as Tanuki Digital news and info. 
Fill out the registration forum using your Asset Store "OR" Order Number here:

http://www.tanukidigital.com/jts_pagoda_architecture/index.php?register=1



----------------------
SUPPORT
----------------------
If you have questions, need help with a feature, or think you've identified a bug please let us know either in the Unity forum or on the Tanuki Digital forum below.

Unity Forum Thread: http://forum.unity3d.com/
Tanuki Digital Forum: http://tanukidigital.com/forum/

You can also email us directly at: konnichiwa@tanukidigital.com



-------------
INSTALLATION
-------------
I. IMPORT FILES INTO YOUR PROJCT
Go to: “Assets -> Import Package -> Custom Package...” in the Unity Menu and select the “JTS_PagodaArchitecture_ver1.x.unitypackage” file. This will open an import dialog box. Click the import button and all files will be imported into your project list under a folder called JTS_PagodaArchitecture.

II. ADD A PREFAB TO YOUR SCENE
There are two category of Prefabs included:

PREFABS
These consist of the normal highres mesh and are located in the 'Prefabs' folder.  These pagoda prefabs are the highest Level of Detail and include physics colliders.  These are located in the "Prefabs" folder.

PREFABS_LOD
These adjust between four LOD levels that automatically switch based on how far away they are from the scene camera.  These are setup with reduced level physics colliders.  These are located in the "Prefabs_LOD" folder.



--------------------------------------------------
DEFERRED Vs. FORWARD (i.e. BLACK RECTANGLE ERRORS)
--------------------------------------------------
By default the Pagoda decals are setup to use a special shader that utlizes Unity's Deferred Rendering pipeline.  This is preferred, and if you can use Deferred in your project then you probably should (switch the rendering path on your scene camera to 'Deferred'.)

If you are instead using Forward rendering, or if you notice black sections on the Pagoda objects then you should switch the decal material to use the Forward shader instead, following these instructions:
1. Go to the 'Materials' folder and click on the 'mat_decal_pagoda_01' material
2. At the top of the Material switch the 'Shader' to use 'TanukiDigital/Decal Forward'




-------------------------------
RELEASE NOTES - Version 1.0
-------------------------------
- Initial Release.
