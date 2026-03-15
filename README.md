# AccesClientWPF

AccesClientWPF est une application WPF conçue pour faciliter la gestion des clients et des connexions à distance. Elle permet de gérer des accès RDS, VPN et AnyDesk, de lancer des connexions sécurisées, et de consulter facilement les fichiers associés à chaque client dans une interface moderne, claire et adaptée à un usage quotidien.

## Fonctionnalités principales

- Gestion des clients et des fichiers associés
- Connexion à distance via RDP avec gestion des comptes utilisateurs
- Connexion VPN via exécutable configuré
- Gestion des accès AnyDesk
- Organisation des éléments par type : RDS, VPN, AnyDesk
- Ajout d’acronymes sur les clients pour une identification plus rapide
- Fiche de fin de développement intégrée
- Amélioration de la gestion multi-écran
- Interface moderne avec un thème inspiré de Fluent 2

## Nouveautés de la version 1.7.3

- Refonte visuelle de l’application avec un nouveau thème type Fluent 2
- Ajout de la fonctionnalité de fiche de fin de développement
- Amélioration de la gestion multi-écran
- Ajout de l’acronyme sur les clients

## Installation

### Prérequis

- Windows
- Runtime .NET compatible avec l’application
- Fichiers de release du projet

### Étapes

1. Télécharger la dernière release depuis GitHub
2. Extraire les fichiers si nécessaire
3. Exécuter le fichier `.exe`

## Utilisation

### Ajouter et gérer un client

- L’interface affiche la liste des clients dans une barre latérale
- Chaque client peut contenir différents types d’éléments : RDS, VPN, AnyDesk
- Les acronymes clients permettent un repérage plus rapide dans l’interface

### Gérer les comptes et accès

- Utilisez l’interface dédiée pour ajouter ou modifier les informations de connexion
- Selon le type d’élément, les informations demandées sont adaptées :
  - RDS : IP ou DNS et mot de passe
  - AnyDesk : identifiant de connexion et mot de passe facultatif
  - VPN : chemin de l’exécutable via sélection de fichier

### Lancer une connexion

- Sélectionnez l’élément souhaité dans la liste
- Lancez la connexion correspondante directement depuis l’application

### Gérer les fichiers associés

- Chaque client dispose de son propre répertoire
- Les fichiers sont triés par type pour une consultation plus simple
- Les éléments peuvent être ouverts directement depuis l’interface

### Fiche de fin de développement

- Une fiche de fin de développement peut être ajoutée et consultée pour faciliter le suivi et la clôture des interventions ou développements réalisés

## Interface

AccesClientWPF propose une interface WPF modernisée avec :

- un thème visuel inspiré de Fluent 2
- une meilleure lisibilité des informations
- une navigation plus fluide
- une prise en charge améliorée des environnements multi-écrans

## Version actuelle

Version : 1.7.3

Cette version inclut notamment :

- le nouveau thème visuel type Fluent 2
- la gestion des accès RDS, VPN et AnyDesk
- l’ajout des acronymes clients
- la fonctionnalité de fiche de fin de développement
- une meilleure gestion multi-écran
- des améliorations générales de l’ergonomie et de l’interface

## Contributions

Les contributions sont les bienvenues.

### Étapes proposées

1. Forker le dépôt
2. Créer une branche dédiée à la fonctionnalité :
   `git checkout -b feature/ma-nouvelle-fonctionnalite`
3. Commiter les modifications :
   `git commit -m "Ajout d'une nouvelle fonctionnalité"`
4. Pousser la branche :
   `git push origin feature/ma-nouvelle-fonctionnalite`
5. Ouvrir une Pull Request sur GitHub

## Licence

Aucune licence n’est fournie dans ce dépôt pour le moment.

## Auteur

Antunes Barata Rodrigue  
Développement principal
