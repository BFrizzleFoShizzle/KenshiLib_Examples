#pragma once

#include <kenshi/Appearance.h>
#include <kenshi/InputHandler.h>
#include <kenshi/Globals.h>
#include <kenshi/Character.h>
#include <kenshi/GameWorld.h>
#include <kenshi/PlayerInterface.h>
#include <kenshi/Faction.h>
#include <kenshi/FactionRelations.h>
#include <kenshi/KingOfRenderThread.h>
#include <kenshi/Renderer.h>
#include <kenshi/RenderToTextureotron.h>
#include <kenshi/Appearance.h>
#include <core/Functions.h>
#include <ogre/OgreMaterial.h>
#include <ogre/OgreTechnique.h>
#include <ogre/OgreEntity.h>
#include <ogre/OgreMaterialManager.h>
#include <ogre/OgreSharedPtr.h>
#include <Debug.h>

void EnableHighlight(Character* character)
{
	// isEnemy if enemy with at least one character
	bool isEnemy = false;
	for (int i = 0; i < ou->player->playerCharacters.size() && !isEnemy; ++i)
		isEnemy = character->isEnemy(ou->player->playerCharacters[i], true);
	//for (int i = 0; i < thisptr->player->playerCharacters.size() && !isEnemy; ++i)
	//	isEnemy = (*iter)->shouldIScrewThisGuyOver(thisptr->player->playerCharacters[i]);
	//for (int i = 0; i < thisptr->player->playerCharacters.size() && !isEnemy; ++i)
	//	isEnemy = (*iter)->areYouGonnaGetMe(thisptr->player->playerCharacters[i]);
	for (int i = 0; i < ou->player->playerCharacters.size() && !isEnemy; ++i)
		isEnemy = ou->player->playerCharacters[i]->areYouGonnaGetMe(character);

	// isAlly if allied with all characters
	bool isAlly = true;
	for (int i = 0; i < ou->player->playerCharacters.size() && isAlly; ++i)
		isAlly = character->isAlly(ou->player->playerCharacters[i], true);

	// if neither, colour based on relations
	// scale -100-100 to -1-1
	float factionRelations = (character->getFaction()->relations->getFactionRelation(ou->player->getFaction()) / 100.0f);

	if (character->isPlayerCharacter())
		character->getAppearance()->bodyMaterial->getTechnique(0)->getPass(0)->getFragmentProgramParameters()->setNamedConstant("coloroverride", Ogre::ColourValue(0.0, 0.0, 1.0, 1.0));
	else if (isEnemy)
		character->getAppearance()->bodyMaterial->getTechnique(0)->getPass(0)->getFragmentProgramParameters()->setNamedConstant("coloroverride", Ogre::ColourValue(1.0, 0.0, 0.0, 1.0));
	else if (isAlly)
		character->getAppearance()->bodyMaterial->getTechnique(0)->getPass(0)->getFragmentProgramParameters()->setNamedConstant("coloroverride", Ogre::ColourValue(0.0, 1.0, 0.0, 1.0));
	else
		character->getAppearance()->bodyMaterial->getTechnique(0)->getPass(0)->getFragmentProgramParameters()->setNamedConstant("coloroverride", Ogre::ColourValue(std::max(0.0f, -factionRelations + 1.0f), factionRelations + 1, 0.0, 1.0));

	character->getAppearance()->bodyMaterial->getTechnique(0)->getPass(0)->getVertexProgramParameters()->setNamedConstant("overrideDepth", true);
}

void (*GameWorld__mainLoop_GPUSensitiveStuff_orig)(GameWorld* thisptr, float time);
void GameWorld__mainLoop_GPUSensitiveStuff_hook(GameWorld* thisptr, float time)
{
	if(key->highlight)
	{
		const ogre_unordered_set<Character*>::type& characters = thisptr->getCharacterUpdateList();
		for (ogre_unordered_set<Character*>::type::iterator iter = characters.begin(); iter != characters.end(); ++iter)
		{
			if (!(*iter)->getAppearance()->bodyMaterial.isNull())
			{
				EnableHighlight(*iter);
			}
		}
	}
	else
	{
		const ogre_unordered_set<Character*>::type& characters = thisptr->getCharacterUpdateList();
		for (ogre_unordered_set<Character*>::type::iterator iter = characters.begin(); iter != characters.end(); ++iter)
		{
			if (!(*iter)->getAppearance()->bodyMaterial.isNull())
			{
				(*iter)->getAppearance()->bodyMaterial->getTechnique(0)->getPass(0)->getFragmentProgramParameters()->setNamedConstant("coloroverride", Ogre::ColourValue(1.0, 1.0, 1.0, 0.0));
				(*iter)->getAppearance()->bodyMaterial->getTechnique(0)->getPass(0)->getVertexProgramParameters()->setNamedConstant("overrideDepth", false);
			}
		}
	}

	GameWorld__mainLoop_GPUSensitiveStuff_orig(thisptr, time);
}

// fix for dead bodies not updating
void (*GameWorld_removeFromUpdateListMain_orig)(GameWorld* thisptr, Character* character);
void GameWorld_removeFromUpdateListMain_hook(GameWorld* thisptr, Character* character)
{
	if (!character->getAppearance()->bodyMaterial.isNull())
	{
		character->getAppearance()->bodyMaterial->getTechnique(0)->getPass(0)->getFragmentProgramParameters()->setNamedConstant("coloroverride", Ogre::ColourValue(1.0, 1.0, 1.0, 0.0));
		character->getAppearance()->bodyMaterial->getTechnique(0)->getPass(0)->getVertexProgramParameters()->setNamedConstant("overrideDepth", false);
	}
	GameWorld_removeFromUpdateListMain_orig(thisptr, character);
}

// fix to stop highlight applying to character portraits
void (*RenderToTextureotron_renderCharacter_orig)(RenderToTextureotron* thisptr, Character* character, Ogre::SharedPtr<Ogre::Texture> texture, const iVector2& size, const Ogre::TRect<float>& rect);
void RenderToTextureotron_renderCharacter_hook(RenderToTextureotron* thisptr, Character* character, Ogre::SharedPtr<Ogre::Texture> texture, const iVector2& size, const Ogre::TRect<float>& rect)
{
	if (key->highlight)
	{
		// disable highlight
		if (!character->getAppearance()->bodyMaterial.isNull())
		{
			character->getAppearance()->bodyMaterial->getTechnique(0)->getPass(0)->getFragmentProgramParameters()->setNamedConstant("coloroverride", Ogre::ColourValue(1.0, 1.0, 1.0, 0.0));
			character->getAppearance()->bodyMaterial->getTechnique(0)->getPass(0)->getVertexProgramParameters()->setNamedConstant("overrideDepth", false);
		}
	}
	RenderToTextureotron_renderCharacter_orig(thisptr, character, texture, size, rect);
	if (key->highlight)
	{
		// re-enable highlight
		EnableHighlight(character);
	}
}

__declspec(dllexport) void startPlugin()
{
	if (KenshiLib::SUCCESS != KenshiLib::AddHook(KenshiLib::GetRealAddress(&GameWorld::_NV_mainLoop_GPUSensitiveStuff), &GameWorld__mainLoop_GPUSensitiveStuff_hook, &GameWorld__mainLoop_GPUSensitiveStuff_orig))
		ErrorLog("Character Highlight: could not install hook!");
	if (KenshiLib::SUCCESS != KenshiLib::AddHook(KenshiLib::GetRealAddress(&GameWorld::removeFromUpdateListMain), &GameWorld_removeFromUpdateListMain_hook, &GameWorld_removeFromUpdateListMain_orig))
		ErrorLog("Character Highlight: could not install hook!");
	if (KenshiLib::SUCCESS != KenshiLib::AddHook(KenshiLib::GetRealAddress(&RenderToTextureotron::renderCharacter), &RenderToTextureotron_renderCharacter_hook, &RenderToTextureotron_renderCharacter_orig))
		ErrorLog("Character Highlight: could not install hook!");
}