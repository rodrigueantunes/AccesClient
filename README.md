# AccesClientWPF

**AccesClientWPF** est une application **WPF** (MVVM) pour centraliser tout ce qui concerne l’accès aux postes/serveurs des clients : connexions **RDS**, **AnyDesk**, **VPN**, ouverture de **dossiers/fichiers**, ainsi que la gestion de **mots de passe supplémentaires**.
Elle offre une base locale (`database.json`) et une **base partagée** (fichier `.antclient`) avec **verrouillage** pour éviter les conflits.

---

# Sommaire

* [Fonctionnalités](#fonctionnalités)
* [Types d’entrées gérées](#types-dentrées-gérées)
* [Base locale & base partagée](#base-locale--base-partagée)
* [Installation](#installation)
* [Prise en main](#prise-en-main)
* [Détails techniques](#détails-techniques)
* [Sécurité des identifiants](#sécurité-des-identifiants)
* [Dépannage](#dépannage)
* [Compilation / Contribuer](#compilation--contribuer)
* [Licence & Auteurs](#licence--auteurs)

---

# Fonctionnalités

* **Gestion des clients** (barre latérale) :

  * Ajout / suppression / ré-ordonnancement.
  * Sélection du client pour filtrer ses éléments.

* **Éléments par client** (colonne centrale) :

  * **RDS** : génération d’un `.rdp` temporaire avec **multi-écran** optionnel, **titre** de session, stockage/cleanup **cmdkey** pour les identifiants.
  * **AnyDesk** : ouverture via `anydesk.exe` (détection/config du chemin), prise en charge d’un **mot de passe** et d’un **couple Windows (user/mdp)**.
  * **VPN** : lancement d’un exécutable (simulation FortiClient).
  * **Dossier** / **Fichier** : ouverture directe, **icône personnalisée** possible pour les fichiers.

* **Mots de passe supplémentaires** (colonne droite) :

  * Type dédié **`MotDePasse`** (non lié à `FullPath`).
  * Ajout, **Modifier**, **Monter/Descendre**, **Supprimer** via menu contextuel.
  * Boutons 📋 pour **copier** user/mot de passe (avec **déchiffrement** automatique).
  * **Affichage masquable** des identifiants (case « Afficher les identifiants »).

* **Base partagée** (`.antclient`) :

  * **Ouverture / création / sauvegarde / *Save As***.
  * **Verrouillage** via fichier `.lock` (empêche l’ouverture par plusieurs personnes, affiche l’utilisateur).
  * **Import vers la base locale** (`database.json`) avec **fusion** (ajout/mise à jour).

* **Confort d’utilisation** :

  * **Menu contextuel** sur la liste centrale (Ajouter / Modifier / **Tester** / Supprimer).
  * **Multi-moniteur** RDS (case à cocher globale).
  * **Affichage des identifiants** (case à cocher globale).
  * **Icônes par type** conservées (pack URIs) + **icône personnalisée** pour `Fichier`.

---

# Types d’entrées gérées

| Type           | Champs principaux                                                                             | Action par double-clic / Test                                |
| -------------- | --------------------------------------------------------------------------------------------- | ------------------------------------------------------------ |
| **RDS**        | `IP/DNS`, `Utilisateur`, `Mot de passe` (chiffré)                                             | Lance `mstsc` avec `.rdp` temp, `cmdkey`, titre, multi-écran |
| **AnyDesk**    | `AnyDesk ID`, `Mot de passe AnyDesk` (chiffré) + **Windows** `User/Mdp` (chiffré, facultatif) | Lance `anydesk.exe` (détection du chemin, persistant)        |
| **VPN**        | Chemin de l’exécutable                                                                        | Lance l’exécutable                                           |
| **Dossier**    | Chemin du dossier                                                                             | Ouvre dans **Explorer**                                      |
| **Fichier**    | Chemin du fichier + **icône personnalisée** (png/jpg/…)                                       | Ouvre le fichier                                             |
| **MotDePasse** | *Sans `FullPath`* — **User/Mdp** (chiffré) uniquement                                         | — (gestion et copie dans le panneau droit)                   |

---

# Base locale & base partagée

* **Base locale** : `database.json` (à côté de l’exe).
* **Base partagée** : fichier `*.antclient` (JSON), **verrouillé** par un `*.lock` lors de l’ouverture.

  * **Ouvrir** une base partagée.
  * **Sauvegarder** / **Enregistrer sous**.
  * **Importer vers la base locale** (fusion Clients/Files, mise à jour si entrée existante).

> L’ordre des éléments **centre** et des mots de passe **droite** est **persisté** à l’enregistrement.

---

# Installation

1. **Prérequis**

   * Windows 10/11 (x64).
   * **.NET Desktop Runtime** installé (version correspondant à votre build, ex. .NET 6/7).
   * **AnyDesk** installé si vous comptez l’utiliser (ou sélection du chemin à la première utilisation).

2. **Depuis la Release**

   * Téléchargez l’archive depuis l’onglet **Releases**.
   * Dézippez, puis lancez l’exécutable.

3. **Fichiers créés automatiquement**

   * `database.json` : base locale.
   * `settings.json` : paramètres (ex. chemin d’AnyDesk).
   * `rds_accounts.json` : comptes RDS (si vous utilisez la fenêtre dédiée).

---

# Prise en main

1. **Créer/choisir un client**
   Barre latérale gauche → **Gérer les clients**.
   Sélectionnez un client pour afficher ses éléments.

2. **Ajouter un élément**
   Bouton **+ Ajouter** (bandeau central) ou clic droit → **Ajouter**.
   Choisissez le **Type** (RDS/AnyDesk/VPN/Dossier/Fichier/**MotDePasse**).

   * Pour **MotDePasse**, le type est **forcé** et le formulaire dédié s’affiche.

3. **Modifier / Supprimer / Tester** (liste centrale)
   Clic droit sur un élément → **Modifier** / **Tester** / **Supprimer**.

   * **Tester** :

     * RDS : crée un `.rdp` temporaire, applique le **multi-écran** si coché.
     * AnyDesk : lance `anydesk.exe` (mot de passe transmis si présent).
     * VPN : lance l’exécutable.
     * Dossier/Fichier : ouvre directement.

4. **Mots de passe supplémentaires** (colonne droite)

   * **+ Ajout** (bouton orange) → type **MotDePasse** forcé.
   * **Modifier/Monter/Descendre/Supprimer** via **menu contextuel**.
   * Boutons 📋 pour **copier** l’utilisateur/le mot de passe.
   * Case **Afficher les identifiants** pour masquer/afficher les valeurs.

5. **Base partagée**

   * **Ouvrir base partagée** (`*.antclient`) → un fichier `.lock` est créé.
     Si le fichier est déjà ouvert ailleurs, l’appli vous l’indique.
   * **Sauvegarder / Enregistrer sous** pour persister les modifications.
   * **Importer vers base principale** pour fusionner dans `database.json`.

---

# Détails techniques

## Architecture

* **MVVM**

  * `ViewModels` : `MainViewModel`, `SharedDatabaseViewModel`, `RdsAccountViewModel`
  * `Views` : `MainWindow`, `SharedDatabaseWindow`, `AddEntryWindow`, `SharedClientManagementWindow`, `RdsAccountWindow`, etc.
* **Helpers**

  * `EncryptionHelper` (AES), `ClipboardHelper` (CF\_TEXT/CF\_UNICODETEXT, persistant),
  * `AppSettings` (JSON `settings.json` pour le chemin d’AnyDesk),
  * `DatabaseHelper` (lecture/écriture JSON).
* **Converters**

  * `FileTypeToIconConverter` (pack URIs + icône perso pour `Fichier`),
  * `DecryptPasswordConverter`, `StringToVisibilityConverter`, `NullOrEmptyToVisibilityConverter`.

## RDS

* Génère un `.rdp` **temporaire** (titre, multi-écran, etc.).
* Enregistre les identifiants via `cmdkey.exe` avant de lancer `mstsc`, puis **nettoie** après délai.

## AnyDesk

* Détection/validation du chemin `anydesk.exe` (via `AppSettings` → `settings.json`).
* Lancement avec mot de passe (`--with-password`) si renseigné.

## Données & fichiers

* `database.json` (local), `*.antclient` (partagé), `*.lock` (verrou).
* `rds_accounts.json` (si vous utilisez la fenêtre comptes RDS).
* Icônes (pack URIs) : `remote_desktop.png`, `vpn.png`, `anydesk.png`, `dossier.png`, `fichier.png`, `default.png`.

---

# Sécurité des identifiants

* Les mots de passe sont **chiffrés localement (AES)** avant stockage.
* À l’usage :

  * **RDS** : injectés via `cmdkey` puis **effacés** après un délai.
  * **AnyDesk** : transmis au processus de connexion si présent.
* ⚠️ Pour un usage fortement sensible, préférez **Windows Credential Manager/DPAPI** et des clés hors binaire.

---

# Dépannage

* **Le menu contextuel est grisé**
  → Sélectionnez un **client** (la plupart des actions sont liées au client courant).
* **AnyDesk ne se lance pas / chemin invalide**
  → Au premier lancement, l’appli vous propose de **sélectionner `anydesk.exe`**. Le chemin est mémorisé.
* **Impossible d’ouvrir la base partagée**
  → Un fichier `.lock` existe : la base est déjà ouverte. Fermez-la ailleurs, ou supprimez le `.lock` si vous êtes sûr qu’elle n’est plus utilisée.
* **Les mots de passe ne s’affichent pas**
  → Décochez/recochez **Afficher les identifiants**. Pour copier, utilisez les **boutons 📋**.
* **Ordre des éléments non conservé**
  → Pensez à **Sauvegarder** la base partagée pour persister l’ordre actuel.

---

# Compilation / Contribuer

## Compiler

* **Visual Studio 2022** (ou plus récent) + **SDK .NET** correspondant.
* Packages NuGet :

  * `Newtonsoft.Json`
* Démarrez le projet **WPF** (x64 recommandé).

## Contributions

1. Forkez le dépôt.
2. Créez une branche : `git checkout -b feature/ma-nouvelle-fonctionnalite`.
3. Committez : `git commit -m "Ajout: <votre fonctionnalité>"`.
4. Poussez : `git push origin feature/ma-nouvelle-fonctionnalite`.
5. Ouvrez une **Pull Request**.

---

# Licence & Auteurs

* **Licence** : MIT
* **Auteur** : **Antunes Barata Rodrigue** – Développement principal

---

> **Version actuelle : v1.4.1**
> Points clés : panneau **Mots de passe** enrichi (ajout/édition/ordre/copie), **test** des éléments, **verrou** de base partagée, **chemin AnyDesk** persistant, **icônes** conservées, gestion **multi-moniteur** RDS, et **fusion** vers la base locale.
