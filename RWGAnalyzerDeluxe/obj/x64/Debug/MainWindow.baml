   M S B A M L   `   `   `  ����L  HRWGAnalyzerDeluxe, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null5clr-namespace:RWGAnalyzerDeluxeRWGAnalyzerDeluxe  5   	   "    RWGAnalyzerDeluxe.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   1localclr-namespace:RWGAnalyzerDeluxe     5   	     � Title$  RWGAnalyzer��5   	   $	��768��6   $
��1024��6*   .��5	      � -   5        $��AppGrid��5	      .+�5
   
   �� -   5        $��TextBlockSummary��5
      $
��Left=�6,   $��	25,25,0,0q�6G   $
��Wrap=�6Z   $��	TextBlock��6n   $	��Top=�6   $��65��6�   $	��629��6�   $��Courier New)�6�   �� 5   
   -   5        $��ChooseWorldButton��5      $��Choose World...��6*   $
��Left=�6D   $��25,0,0,0q�6_   $	��Top=�6q   $��98��6�   6�   � 5   
   -   5        $��ResultsGrid��5      $
��Left=�6"   $	��590��6=   $��	25,95,0,0q�6J   $	��Top=�6]   $	��590��6u   �� 5   
   -   5         ��Name$ PrefabSummary��5      $
��Left=�6+   $��
654,25,0,0q�6F   $
��Wrap=�6Z   $	��Top=�6n   $	��212��6�   $	��328��6�   $��Courier New)�6�   �� 6�   $	�Tex��6�   �� 6�   $�tBlock��6�   6�   � 5   
   -   5        $��
LegendGrid��5      $
��Left=�6!   $	��275��6<   $��654,410,0,0q�6I   $	��Top=�6^   $	��328��6v   �� 5   
   -   5        $��TextBlockStatus��5      $��20(�6+   $
��Left=�6:   $��
654,-2,0,0q�6U   $
��Wrap=�6i   $��ABCDEF��6}   $	��Top=�6�   $��34��6�   $	��323��6�   
���  ��$��Oblique$�6�   �� 5   
   -   5        $��RefreshButton��5      {�.  $��Refresh��68   $
��Left=�6J   $��654,690,0,0q�6e   $	��Top=�6z   $��75��6�   $��25��6�   6�   � 5   
   -	   5        $��
BiomesGrid��5      $
��Left=�6"   $	��163��6=   $��654,242,0,0q�6J   $	��Top=�6_   $	��328��6w    � ColumnDefinitions 5      �� 5      $	��51* �6#   �� 5      $
��277* �6#   5      �� 5   
   -
   5        $��SliderGridFactor��5   X    �� IsSnapToTickEnabled .  � Value$ 10Q�6p    � Minimum$ 4Q�6{    � Maximum$ 24Q�6�    � SmallChange$ 1Q�6�   $
��Left=�6�   $��203,697,0,0q�6�   $	��Top=�6�   $	��168��6�   $��40��6�    �� TickPlacement$ BottomRight=�6  	 �� TickFrequency$	 2Q�6   
 � LargeChange$
 2Q�62  ���	#FFAC0F0F6B  �� 5   
   $
��Left=�6   $��
25,705,0,0q�6/   $��NoWrap=�6C    �� TextAlignment$ Center=�6Y   $��Set Analysis Grid Scale:��6p   $��Courier New)�6�   $	��178��6�   $��32��6�   $	��Top=�6�   5      5      