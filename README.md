# Mitubishi-robots-2-MQTT

[Student thesis (German)](https://docs.google.com/document/d/1aoB_pFMlUvaGXtW5EZLKQ5L99xn3KzHSzCvQDiuA1Uk) in the 6th theoretical semester of my studies in electrical engineering - automation with [Prof. Dr. Bozena Lamek-Creutz, DHBW](https://www.linkedin.com/in/dr-ing-bozena-lamek-creutz-943766105)

This script interfaces with two Mitsubishi robots, an RV-3SDB an an RH-6SDH5520, that are part of a Festo modular production system located at [DHBW Mannheim](https://www.mannheim.dhbw.de/).
It reads the robot data from TCP-packets using [SharpPcap](https://github.com/dotpcap/sharppcap) and publishes the data to an MQTT-broker using [MQTTnet](https://github.com/dotnet/MQTTnet).

![image](https://user-images.githubusercontent.com/70020564/174442173-d7a7a733-fc2b-4d1a-a4e2-4b4ff1bfa8f1.png)

Robot positions on the MQTT-broker visualized using [MQTT Explorer](https://mqtt-explorer.com). J1 is plotted over time, indicating that the robot performed a repeated motion with a break during that timeframe:

![172181235-3e573d67-2956-4d0b-a75e-a40fb8ceff73](https://user-images.githubusercontent.com/70020564/174440337-8a9e495b-238a-4ea0-86cb-060ded494b48.png)
