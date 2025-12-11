using System;
using NavigationLib.Adapters;

namespace NavigationLib.UseCases
{
    /// <summary>
    ///     �ɯ�y�{�һݪ� Region ������H�C
    /// </summary>
    /// <remarks>
    ///     �������w�q�F Use Cases �h�ާ@ UI Region �����һݪ��Ҧ���O�A
    ///     �j������� UI Framework�]�p WPF�^���̿�C
    ///     ��@�̳q�`��� Adapters �� FrameworksAndDrivers �h�C
    /// </remarks>
    internal interface IRegionElement
    {
        /// <summary>
        ///     ���o������ DataContext�C
        /// </summary>
        /// <returns>DataContext ����A�Y���]�w�h�� null�C</returns>
        object GetDataContext();

        /// <summary>
        ///     �q�\ DataContext �ܧ�ƥ�C
        /// </summary>
        /// <param name="handler">DataContext �ܧ�ɪ��B�z�`���C</param>
        void AddDataContextChangedHandler(EventHandler handler);

        /// <summary>
        ///     �����q�\ DataContext �ܧ�ƥ�C
        /// </summary>
        /// <param name="handler">�n�������B�z�`���C</param>
        void RemoveDataContextChangedHandler(EventHandler handler);

        /// <summary>
        ///     ���o�������� Dispatcher�A�Ω�N�ާ@�իר� UI ������C
        /// </summary>
        /// <returns>IDispatcher ������ҡC</returns>
        IDispatcher GetDispatcher();

        /// <summary>
        ///     �ˬd�������O�_���b��ı�𤤡C
        /// </summary>
        /// <returns>�Y�������b��ı�𤤫h�� true�A�_�h�� false�C</returns>
        bool IsInVisualTree();

        /// <summary>
        ///     �ˬd�� Region �����O�_�P�t�@�� Region �����]�ˬۦP�����h UI �����C
        /// </summary>
        /// <param name="other">�n��諸�t�@�� Region �����C</param>
        /// <returns>�Y��̥]�ˬۦP�����h�����h�� true�A�_�h�� false�C</returns>
        /// <remarks>
        ///     ����k�Ω�P�_��Ӥ��P�� IRegionElement ��ҬO�_�N���P�@�� UI �����A
        ///     �קK���Ƶ��U�ۦP�������� Region�C
        /// </remarks>
        bool IsSameElement(IRegionElement other);

        /// <summary>
        ///     �q�\�������}��ı�𪺨ƥ�C
        /// </summary>
        /// <param name="handler">�������}��ı��ɪ��B�z�`���C</param>
        /// <returns>IDisposable ��ҡA�Ω�����q�\�C</returns>
        /// <remarks>
        ///     ����k�Ω� Region �ͩR�g���޲z�A�������q UI ������ɡA
        ///     �i�H�z�L���ƥ�Ĳ�o�M�z�ʧ@�]�p�Ѱ� Region ���U�^�C
        /// </remarks>
        IDisposable SubscribeUnloaded(EventHandler handler);
    }
}
