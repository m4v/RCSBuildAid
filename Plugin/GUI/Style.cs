/* Copyright © 2013-2014, Elián Hanisch <lambdae2@gmail.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using UnityEngine;

namespace RCSBuildAid
{
    public class Style
    {
        Texture2D blankTexture;

        public GUIStyle centerText;
        public GUIStyle smallButton;
        public GUIStyle mainButton;
        public GUIStyle activeButton;
        public GUIStyle sizeLabel;
        public GUIStyle clickLabel;
        public GUIStyle clickLabelCenter;
        public GUIStyle resourceTableName;
        public GUIStyle clickLabelGray;
        public GUIStyle resourceLabel;

        public Style ()
        {
            setupStyle ();
        }

        void setupStyle ()
        {
            /* need a blank texture for the hover effect or it won't work */
            blankTexture = new Texture2D (1, 1, TextureFormat.Alpha8, false);
            blankTexture.SetPixel (0, 0, Color.clear);
            blankTexture.Apply ();

            GUI.skin.label.padding = new RectOffset ();
            GUI.skin.label.wordWrap = false;
            GUI.skin.toggle.padding = new RectOffset (15, 0, 0, 0);
            GUI.skin.toggle.overflow = new RectOffset (0, 0, -1, 0);

            centerText = new GUIStyle (GUI.skin.label);
            centerText.alignment = TextAnchor.MiddleCenter;
            centerText.wordWrap = true;

            smallButton = new GUIStyle (GUI.skin.button);
            smallButton.clipping = TextClipping.Overflow;
            smallButton.fixedHeight = GUI.skin.label.lineHeight;

            mainButton = new GUIStyle (smallButton);

            activeButton = new GUIStyle(mainButton);
            activeButton.normal = mainButton.onNormal;

            float w1, w2;
            GUI.skin.label.CalcMinMaxWidth(new GUIContent ("Size"), out w1, out w2);
            sizeLabel = new GUIStyle (GUI.skin.label);
            sizeLabel.fixedWidth = w2;
            sizeLabel.normal.textColor = GUI.skin.box.normal.textColor;

            resourceTableName = new GUIStyle (GUI.skin.label);
            resourceTableName.normal.textColor = GUI.skin.box.normal.textColor;
            resourceTableName.padding = GUI.skin.toggle.padding;

            clickLabel = new GUIStyle (GUI.skin.button);
            clickLabel.alignment = TextAnchor.LowerLeft;
            clickLabel.padding = new RectOffset ();
            clickLabel.normal = GUI.skin.label.normal;
            clickLabel.hover.background = blankTexture;
            clickLabel.hover.textColor = Color.yellow;
            clickLabel.active = clickLabel.hover;
            clickLabel.fixedHeight = GUI.skin.label.lineHeight;
            clickLabel.clipping = TextClipping.Overflow;

            clickLabelCenter = new GUIStyle (clickLabel);
            clickLabelCenter.alignment = TextAnchor.MiddleCenter;

            clickLabelGray = new GUIStyle(clickLabel);
            clickLabelGray.normal.textColor = GUI.skin.box.normal.textColor;

            resourceLabel = new GUIStyle(GUI.skin.label);
            resourceLabel.padding = GUI.skin.toggle.padding;
        }

    }
}

