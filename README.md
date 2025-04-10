# AccesClientWPF

AccesClientWPF est une application WPF conçue pour faciliter la gestion des clients et des connexions à distance. Elle permet de gérer des comptes RDS, VPN et AnyDesk, de lancer des connexions sécurisées, et de consulter facilement les fichiers associés à chaque client.

## Fonctionnalités principales

- Gestion des clients et des fichiers associés.
- Connexion à distance via RDP avec gestion des comptes utilisateurs.
- Connexion VPN (simulation) via FortiClient.
- Organisation des fichiers par type : RDS, VPN, AnyDesk.
- Interface moderne et intuitive construite avec WPF.

## Installation

1. **Prérequis :**
   - Windows avec .NET Framework ou .NET Core/5+ installé.
   - La Release
   - Exécuter l'exe

## Utilisation

1. **Ajouter un client :**
   - L’interface affiche une liste de clients dans une barre latérale.
   - Cliquez sur “Gérer les comptes de connexion” pour ajouter ou modifier les comptes associés à un client.

2. **Lancer une connexion RDS :**
   - Sélectionnez un fichier RDS dans la liste principale.
   - Double-cliquez pour lancer une connexion RDP en utilisant les informations de connexion configurées.

3. **Gérer les fichiers :**
   - Chaque client dispose de son propre répertoire.
   - Les fichiers sont triés par type et peuvent être ouverts directement depuis l’interface.

4. **Modifier les informations de connexion :**
   - Utilisez la fenêtre de gestion des comptes pour mettre à jour les identifiants ou les descriptions des connexions.

## Version actuelle

**V0.1.1**  
Cette version inclut :
- La gestion des comptes RDS avec cryptage des mots de passe.
- La simulation de connexion VPN.
- Une interface graphique améliorée avec boutons arrondis et icônes personnalisées.
- La gestion des fichiers triés par type (RDS, VPN, AnyDesk).
- L’intégration d’une barre latérale pour sélectionner les clients.

## Contributions

Les contributions sont les bienvenues ! Si vous souhaitez participer, veuillez suivre ces étapes :

1. Forkez ce dépôt.
2. Créez une branche pour votre fonctionnalité : `git checkout -b feature/ma-nouvelle-fonctionnalite`.
3. Ajoutez vos modifications et effectuez un commit : `git commit -m "Ajout d'une nouvelle fonctionnalité"`.
4. Poussez vos modifications : `git push origin feature/ma-nouvelle-fonctionnalite`.
5. Créez une Pull Request sur GitHub.

## Licence

Ce projet est sous licence MIT.

## Auteurs

- **Antunes Barata Rodrigue** - *Développement principal*  
