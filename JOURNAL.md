
# March 21: Schematic draft and initial wiring

Worked on Schematic:
- Made a 5x14 key layout
- Wired switches
- Wired rotary encoder and switch to pico
- Wired LCD

<img width="953" height="619" alt="Journal 2 Screenshot" src="https://github.com/user-attachments/assets/c913975f-f3ec-45e5-b176-8dec315ea2b9" />

**Total time spent: 30 minutes**

# March 22: Schematic overhaul and ERC pass

Completely reworked schematic, added RGB (cuz why not), and 2 rotary encoders. Passes ERC with no errors or warnings.

<img width="1231" height="425" alt="image" src="https://github.com/user-attachments/assets/b634d765-592f-4410-9213-463487910715" />

**Total time spent: 2 hours 25 minutes**

# March 23: Finalized schematic and started PCB routing

Finished up schematic. Began working on PCB layout with 19.5mm spacing (u's) and knocked out most of the routing (turns out I'm bad at layout).

<img width="1538" height="659" alt="image" src="https://github.com/user-attachments/assets/9f88fc3c-2bae-4b7f-b498-c0f2fd9ce8db" />

<img width="872" height="431" alt="image" src="https://github.com/user-attachments/assets/5fd8dd6d-9c03-4fef-b336-4f9b99338d39" />

**Total time spent: 3 hours 10 minutes**

# March 24: Completed PCB routing and DRC checks

Finished up the PCB with 0 DRC errors and double checked the schematic and wiring, as well as the render.

<img width="1552" height="1045" alt="image" src="https://github.com/user-attachments/assets/61830f1f-31cd-4b31-8766-cddf2dc8d339" />

**Total time spent: 45 minutes**

# October 10th : 3D render checks and Onshape case modeling

Finished adding 3D models for all parts of the PCB and checked the render. Also worked a lot on the case design in Onshape.

<img width="1329" height="464" alt="image" src="https://github.com/user-attachments/assets/41511047-047b-4060-b3e1-aa276470146b" />
<img width="1234" height="749" alt="image" src="https://github.com/user-attachments/assets/4d77465a-9c75-4428-88eb-dc0df5398a55" />

**Total time spent: 1 hour**

# October 12th : Added bluetooth and did a lot

Did a lot today, I relized that the keyboard would be a bit better with bluetooth, so I changed out the pi pico for a Pi Pico w, which has bluetooth! Then I remembered I also needed to add a battery for power, then a battery charge controller to easily recharge, then reroute all the wires outside of the no antenna zone, and remake the case to fit the battery and charger(I raised the OLED part a bit which I think still looks pretty good and made that part a lot easier). I did all that, but theres one downside I think is fine which is that RGB is only wiried to work when plugged in, which is fine,![alt text](journal/image.png)![alt text](journal/image-3.png)

**Total time spent: 4.5 hours**

# October 14th : Finalized PCB, finalized CAD, and did code.

Went through and double checked all the PCB connections, checked the BOM, and checked the CAD to make sure it works, then went and made the code using RMKit.![alt text](journal/image.png)
**Total time spent: 1.5 hours**

# October 27th: Created renders and updated Readme
So today I exported the keyboard OBJ file from onshape and imported it into blender, and created a lighting thing and camera to make some renders of it, and I added a couple renders of the keyboard to the readme, as well as added things like the BOM to the readme as well as more desciption.
<img width="2400" height="1600" alt="image" src="https://github.com/user-attachments/assets/e5a247ad-c05a-41d4-b2f7-ce60f790f49a" />

**Total time spent: 45 Minutes**
