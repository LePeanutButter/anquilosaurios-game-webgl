# Power Garden: Juicy Brawl! – Unity WebGL Game Client

[![standard-readme compliant](https://img.shields.io/badge/readme%20style-standard-brightgreen.svg?style=flat-square)](https://github.com/RichardLitt/standard-readme)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=LePeanutButter_anquilosaurios-game-webgl&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=LePeanutButter_anquilosaurios-game-webgl)

> Unity WebGL client for Power Garden: Juicy Brawl! – a multiplayer browser game built for accessibility and scale. 


Power Garden: Juicy Brawl! is a lightweight, multiplayer browser game designed for accessibility, scalability, and social engagement across Latin America. This repository contains the Unity WebGL game client, which integrates with a SvelteKit frontend and ASP.NET backend to deliver immersive gameplay optimized for low-end devices and unstable internet connections.

This repository contains:

1. The Unity WebGL build and source code for Power Garden: Juicy Brawl!.
2. Integration points for Unity Relay, Unity Voice/AI, and RESTful backend APIs.
3. Deployment-ready WebGL export for Azure Blob Storage or App Service.
4. A compliant badge to indicate adherence to the Standard Readme spec.
5. A license under Creative Commons Attribution-NonCommercial-NoDerivs 4.0 International.

Standard Readme is designed for open source libraries. Although Power Garden: Juicy Brawl! is not open-source in the traditional sense, this format ensures clarity, maintainability, and consistency across documentation.

## Table of Contents

- [Background](#background)
- [Install](#install)
- [Usage](#usage)
  - [WebGL Build](#webgl-build)
- [Related Efforts](#related-efforts)
- [Maintainers](#maintainers)
- [Contributing](#contributing)
- [License](#license)

## Background

Power Garden: Juicy Brawl! is a strategic initiative at the intersection of entertainment, technology, and business scalability. Its value proposition includes:

1. **Access to underserved markets**: By removing technical barriers, Power Garden: Juicy Brawl! reaches millions of players excluded from mainstream multiplayer experiences.
2. **Scalable monetization**: The game leverages integrated ads and optional subscriptions.
3. **Modern cloud-ready architecture**: Built on Unity, Svelte, ASP.NET, Redis, MySQL, and Azure, ensuring high performance, low latency, and seamless scalability.

Unity serves as the core engine, enabling WebGL deployment, cross-platform scalability, and integration with Unity Relay and UnityNeuroSpeech services for dynamic multiplayer lobbies and real-time narration.

## Install

Requirements:
- Unity 6.2 (version 6000.2.6f2) – LTS recommended
- Git

Clone the repository and open the project in Unity:

```bash
git clone https://github.com/LePeanutButter/anquilosaurios-game-webgl.git
```

To build for WebGL:

1. Open the project in Unity.
2. Go to **File > Build Settings**.
3. Select **WebGL** and click **Build**.

The output will be located in `/Build/WebGL`.


## Usage

Power Garden: Juicy Brawl! is embedded into the frontend via iframe and communicates with the backend via REST API. Authentication tokens are passed securely using `postMessage()`.

### WebGL Build

The WebGL build is optimized for:

- Low-end devices and browsers
- Fast loading over unstable connections
- Hosting on Azure Blob Storage or App Service


## Related Efforts

- [Unity Relay](https://unity.com/products/unity-relay) – Multiplayer networking
- [UnityNeuroSpeech](https://github.com/HardCodeDev777/UnityNeuroSpeech) – Real-time narration
- [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/) – WebGL hosting
- [Standard Readme](https://github.com/RichardLitt/standard-readme) – Documentation spec

## Maintainers

- [Lanapequin](https://github.com/Lanapequin) – Laura Natalia Perilla Quintero  
- [LePeanutButter](https://github.com/LePeanutButter) – Santiago Botero Garcia  
- [shiro](https://github.com/JoseDavidCastillo) – Jose David Castillo Rodriguez
- [Juana Castillo](https://www.behance.net/placeholder-juana) – Graphic Design & Visual Assets  
- [Camila](https://www.behance.net/placeholder-camila) – Graphic Design & Sprite Illustration

## Contributing

This repository is not open for public contributions. For internal collaboration, please use Azure DevOps or GitHub Issues.

## License

[CC BY-NC-ND 4.0](/LICENSE) © Anquilosaurios Team

> This license allows sharing with attribution, but prohibits commercial use and derivative works.

---

This **README** follows the Standard Readme specification.